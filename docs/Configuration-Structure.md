# D-ASYNC Configuration Structure

D-ASYNC uses .NET configuration extensions, which supports multiple sources like environment variables, JSON files, Azure Key Vault, Azure Configuration Store, and others. For the purpose of this documentation, the configuration settings will be presented in JSON format, which is usually defined in `appsettings.json` file.

The configuration structure is designed to support the utmost flexibility from a global level to the method level, and also at a category level. It is self-repetitive with a few exceptions.

## Base Settings
The base settings define the [common method behavior](Method-Behavior-Options.md), [communication](Communication-Options.md), and [persistence](Persistence-Options.md) mechanism for all services described in the application.
```text
{
    "dasync": {
        // Behavior Settings
        // ...
        
        // Base Communication Settings 
        "communication": {
            "type": "..."
            // Type-Specific Settings
        },

        // Base Persistence Settings 
        "persistence": {
            "type": "..."
            // Type-Specific Settings
        }
    }
}
```

## Base Primitive Type Settings
To override the settings at base level for a concrete communication primitive type, use a sub-section with the name of primitives.
```text
{
    "dasync": {
        "queries":{
            // Queries Behavior Settings
            // ...
            
            // Queries Base Communication Settings 
            "communication": {
                "type": "..."
                // Queries Type-Specific Settings
            }
        },
        "commands": {
            // Commands Behavior Settings
            // ...
            
            // Commands Base Communication Settings 
            "communication": {
                "type": "..."
                // Commands Type-Specific Settings
            },

            // Commands Base Persistence Settings 
            "persistence": {
                "type": "..."
                // Commands Type-Specific Settings
            }
        },
        "events": {
            // Events Behavior Settings
            // ...
            
            // Events Base Communication Settings 
            "communication": {
                "type": "..."
                // Events Type-Specific Settings
            }
        }
    }
}
```
Note that queries and events don't have persistence settings as they must not have any execution state to persist.

## Local Services Settings
For all local services (the ones that have implementation defined besides their contract) use the `dasync:services:_local` section to override the settings.
```text
{
    "dasync": {
        "services": {
            "_local": {
                // Behavior Settings
                // ...
                
                // Base Communication Settings 
                "communication": {
                    "type": "..."
                    // Type-Specific Settings
                },

                // Base Persistence Settings 
                "persistence": {
                    "type": "..."
                    // Type-Specific Settings
                }
            }
        }
    }
}
```
You can override settings for specific primitive types for all local services as follows.
```text
{
    "dasync": {
        "services": {
            "_local": {
                "queries": {
                    // ...
                },
                "commands": {
                    // ...
                },
                "events": {
                    // ...
                }
            }
        }
    }
}
```

## External Services Settings
For all external services (the ones that have contract only without an implementation) use the `dasync:services:_external` section to override the settings.
```text
{
    "dasync": {
        "services": {
            "_external": {
                // Behavior Settings
                // ...
                
                // Base Communication Settings 
                "communication": {
                    "type": "..."
                    // Type-Specific Settings
                },

                // Base Persistence Settings 
                "persistence": {
                    "type": "..."
                    // Type-Specific Settings
                }
            }
        }
    }
}
```
You can override settings for specific primitive types for all external services as follows.
```text
{
    "dasync": {
        "services": {
            "_external": {
                "queries": {
                    // ...
                },
                "commands": {
                    // ...
                },
                "events": {
                    // ...
                }
            }
        }
    }
}
```
The configuration structure for external services is exactly the same as for local ones.

## Specific Service Settings
To override settings for a specific service (local or external) use the `dasync:services:{serviceName}` section (replace `{serviceName}` with an actual name; the name is case-insensitive).
```text
{
    "dasync": {
        "services": {
            "{serviceName}": {
                // Behavior Settings
                // ...
                
                // Base Communication Settings 
                "communication": {
                    "type": "..."
                    // Type-Specific Settings
                },

                // Base Persistence Settings 
                "persistence": {
                    "type": "..."
                    // Type-Specific Settings
                }
            }
        }
    }
}
```
Similarly to local and external services, you can override settings for specific primitive types for a concrete service with one exception - use the `_all` sub-section inside the primitive type section.
```text
{
    "dasync": {
        "services": {
            "{serviceName}": {
                "queries": {
                    "_all": {
                        // ...
                    }
                },
                "commands": {
                    "_all": {
                        // ...
                    }
                },
                "events": {
                    "_all": {
                        // ...
                    }
                }
            }
        }
    }
}
```

## Specific Method Settings
To override settings for a specific method of a service (query of command) use the section with the method name (`dasync:services:{serviceName}:queries:{methodName}` or `dasync:services:{serviceName}:commands:{methodName}`).
```text
{
    "dasync": {
        "services": {
            "{serviceName}": {
                "queries": {
                    "{methodName}": {
                        // Behavior Settings
                        // ...
                        
                        // Base Communication Settings 
                        "communication": {
                            "type": "..."
                            // Type-Specific Settings
                        }
                    }
                },
                "commands": {
                    "{methodName}": {
                        // Behavior Settings
                        // ...
                        
                        // Base Communication Settings 
                        "communication": {
                            "type": "..."
                            // Type-Specific Settings
                        },

                        // Base Persistence Settings 
                        "persistence": {
                            "type": "..."
                            // Type-Specific Settings
                        }
                    }
                }
            }
        }
    }
}
```

## Specific Event Settings
To override settings for a specific event of a service use the section with the event name (`dasync:services:{serviceName}:events:{eventName}`).
```text
{
    "dasync": {
        "services": {
            "{serviceName}": {
                "events": {
                    "{eventName}": {
                        // Behavior Settings
                        // ...
                        
                        // Base Communication Settings 
                        "communication": {
                            "type": "..."
                            // Type-Specific Settings
                        }
                    }
                }
            }
        }
    }
}
```

## Summary
There are 7 levels of settings in the following order of increasing priority: (1) base -> (2) base + primitives -> (3) service category -> (4) service category + primitives -> (5) concrete service -> (6) concrete service + primitives -> (7) concrete service + concrete query, command, or event.

At every single level the structure is similar: [behavior settings](Method-Behavior-Options.md), [communication settings](Communication-Options.md), and [persistence settings](Persistence-Options.md).

## Settings Template
The following template contains all possbile levels described above.
```text
{
    "dasync": {
        // LEVEL 1

        // [behavior settings]
        "communication": {},
        "persistence": {},

        // LEVEL 2
        
        "queries": {
            // [behavior settings]
            "communication": {}
        },
        "commands": {
            // [behavior settings]
            "communication": {},
            "persistence": {}
        },
        "events": {
            // [behavior settings]
            "communication": {}
        },

        "services": {

            // LEVEL 3
        
            "_local": {
                // [behavior settings]
                "communication": {},
                "persistence": {},

                // LEVEL 4
        
                "queries": {
                    // [behavior settings]
                    "communication": {}
                },
                "commands": {
                    // [behavior settings]
                    "communication": {},
                    "persistence": {}
                },
                "events": {
                    // [behavior settings]
                    "communication": {}
                }
            },

            // LEVEL 3
        
            "_extenal": {
                // [behavior settings]
                "communication": {},
                "persistence": {},

                // LEVEL 4
        
                "queries": {
                    // [behavior settings]
                    "communication": {}
                },
                "commands": {
                    // [behavior settings]
                    "communication": {},
                    "persistence": {}
                },
                "events": {
                    // [behavior settings]
                    "communication": {}
                }
            },

            // LEVEL 5
        
            "{serviceName}": {
                // [behavior settings]
                "communication": {},
                "persistence": {},

                "queries": {
        
                    // LEVEL 6
        
                    "_all": {
                        // [behavior settings]
                        "communication": {}
                    },

                    // LEVEL 7
        
                    "{methodName}": {
                        // [behavior settings]
                        "communication": {}
                    }
                },
                "commands": {

                    // LEVEL 6
        
                    "_all": {
                        // [behavior settings]
                        "communication": {},
                        "persistence": {}
                    },

                    // LEVEL 7

                    "{methodName}": {
                        // [behavior settings]
                        "communication": {},
                        "persistence": {}
                    }
                },
                "events": {

                    // LEVEL 6
        
                    "_all": {
                        // [behavior settings]
                        "communication": {}
                    },

                    // LEVEL 7

                    "{eventName}": {
                        // [behavior settings]
                        "communication": {}
                    }
                }
            }
        }
    }
}
``` 