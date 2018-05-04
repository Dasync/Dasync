using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.FabricConnector.AzureStorage
{
    public class MultipartMessageWriter : IDisposable
    {
        internal static readonly Encoding UTF8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private Stream _stream;
        private TextWriter _writer;
        private ISerializer _serializer;
        private ITextSerializer _textSerializer;
        private string _delimiterStart;
        private bool _hasWrittenData;

        public MultipartMessageWriter(Stream stream, ISerializer serializer, long delimiterId)
            : this(new StreamWriter(stream, UTF8, 4096, leaveOpen: true), serializer, delimiterId)
        {
            _stream = stream;
        }

        public MultipartMessageWriter(TextWriter writer, ISerializer serializer, long delimiterId)
        {
            _writer = writer;
            _serializer = serializer;
            _textSerializer = serializer as ITextSerializer;
            _delimiterStart = "##" + delimiterId.ToString("X16") + "#";
        }

        public void Write(string key, object value)
        {
            if (value == null)
                return;

            if (_hasWrittenData)
                _writer.Write('\n');

            _writer.Write(_delimiterStart);
            _writer.Write(key);
            _writer.Write("##\n");

            if (_textSerializer != null)
            {
                _textSerializer.Serialize(_writer, value);
            }
            else if (_stream == null)
            {
                using (var buffer = new MemoryStream())
                {
                    _serializer.Serialize(buffer, value);
                    _writer.Write(Convert.ToBase64String(buffer.ToArray()));
                }
            }
            else
            {
                _writer.Flush();
                _serializer.Serialize(_stream, value);
            }

            _hasWrittenData = true;
        }

        public void Dispose()
        {
            _writer?.Dispose();
            _writer = null;
        }
    }

    public class MultipartMessageReader
    {
        private struct PartInfo
        {
            public int IdOffset;
            public int IdLength;
            public int DataOffset;
            public int DataLength;
            public string Name;
        }

        private byte[] _data;
        private readonly ISerializer _serialier;
        private Dictionary<string, PartInfo> _parts;

        public MultipartMessageReader(byte[] data, ISerializer serialier)
        {
            _data = data;
            _serialier = serialier;
            Scan();
        }

        public bool TryGetPart(string name, out Stream stream)
        {
            if (_parts != null && _parts.TryGetValue(name, out var partInfo))
            {
                stream = new MemoryStream(_data, partInfo.DataOffset, partInfo.DataLength, writable: false);
                return true;
            }
            stream = null;
            return false;
        }

        public bool TryGetValue<T>(string name, out T value) where T : new()
        {
            if (TryGetPart(name, out var stream))
            {
                using (stream)
                {
                    if (stream.Length > 0)
                    {
                        value = _serialier.Deserialize<T>(stream);
                        return true;
                    }
                }
            }
            value = default(T);
            return false;
        }

        public bool TryPopulate(string name, IValueContainer target)
        {
            if (TryGetPart(name, out var stream))
            {
                using (stream)
                {
                    if (stream.Length > 0)
                    {
                        _serialier.Populate(stream, target);
                        return true;
                    }
                }
            }
            return false;
        }

        private void Scan()
        {
            if (_data == null || _data.Length == 0)
                return;

            _parts = new Dictionary<string, PartInfo>();

            if (_data.Length < 7)
                throw new Exception("invalid data");

            if (_data[0] != '#' && _data[1] != '#')
                throw new Exception("invalid data");

            if (!TryFindDoubleHash(_data, 2, out var searchIndex))
                throw new Exception("invalid data");

            var currentPartInfo = new PartInfo
            {
                DataOffset = searchIndex + 2
            };
            ParsePartInfo(_data, 2, searchIndex, ref currentPartInfo, skipId: false);
            searchIndex = currentPartInfo.DataOffset;

            while (true)
            {
                int indexAfterId;
                if (TryFindDoubleHash(_data, searchIndex, out var startIndex)
                    && _data[startIndex - 1] == '\n'
                    && ArePartIdsEqual(_data, startIndex + 2, ref currentPartInfo)
                    && TryFindDoubleHash(_data,
                        indexAfterId = (startIndex + 3 + currentPartInfo.IdLength),
                        out var endIndex))
                {
                    currentPartInfo.DataLength = startIndex - 1 - currentPartInfo.DataOffset;
                    _parts.Add(currentPartInfo.Name, currentPartInfo);

                    currentPartInfo = new PartInfo
                    {
                        IdOffset = startIndex + 2,
                        IdLength = currentPartInfo.IdLength,
                        DataOffset = endIndex + 2
                    };
                    ParsePartInfo(_data, indexAfterId, endIndex, ref currentPartInfo, skipId: true);
                    searchIndex = currentPartInfo.DataOffset;
                }
                else
                {
                    currentPartInfo.DataLength = _data.Length - currentPartInfo.DataOffset;
                    _parts.Add(currentPartInfo.Name, currentPartInfo);
                    break;
                }
            }
        }

        private static bool TryFindDoubleHash(byte[] data, int offset, out int index)
        {
            var bound = data.Length - 1;
            for (; offset < bound; offset++)
            {
                if (data[offset] == '#' && data[offset + 1] == '#')
                {
                    index = offset;
                    return true;
                }
            }
            index = -1;
            return false;
        }

        private static bool ArePartIdsEqual(byte[] data, int offset, ref PartInfo comparand)
        {
            for (int i = offset, n = 0, j = comparand.IdOffset;
                i < data.Length && n < comparand.IdLength;
                i++, n++, j++)
            {
                if (data[i] != data[j])
                    return false;
            }
            if (data[offset + comparand.IdLength] != '#')
                return false;
            return true;
        }

        private static void ParsePartInfo(byte[] data, int start, int end, ref PartInfo partInfo, bool skipId)
        {
            var state = skipId ? 1 : 0;
            var lastStart = start;
            for (var i = start; i <= end; i++)
            {
                if (data[i] == '#' || i == end)
                {
                    if (state == 0)
                    {
                        partInfo.IdOffset = lastStart;
                        partInfo.IdLength = i - lastStart;
                        state = 1;
                    }
                    else if (state == 1)
                    {
                        var length = i - lastStart;
                        partInfo.Name = MultipartMessageWriter.UTF8.GetString(data, lastStart, length);
                        state = -1;
                    }
                    else
                    {
                        throw new Exception("invalid part header");
                    }
                    lastStart = i + 1;
                }
            }
        }
    }
}
