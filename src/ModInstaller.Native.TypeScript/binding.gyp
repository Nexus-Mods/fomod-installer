{
    "targets": [
        {
            "target_name": "modinstaller",
            "sources": [
                "src-native/main.cpp"
            ],
            "include_dirs": [
                "<!@(node -p \"require('node-addon-api').include\")",
                "<(module_root_dir)"
            ],
            "conditions": [
                ["OS=='win'", {
                    "libraries": [
                        "<(module_root_dir)/ModInstaller.Native.lib"
                    ]
                }],
                ["OS=='linux'", {
                    "libraries": [
                        "-L<(module_root_dir)",
                        "-l:ModInstaller.Native.so"
                    ],
                    "ldflags": [
                        "-Wl,-rpath,<(module_root_dir)"
                    ]
                }]
            ],
            "defines": [
                "NAPI_CPP_EXCEPTIONS",
                "_SILENCE_CXX17_CODECVT_HEADER_DEPRECATION_WARNING"
            ],
            "cflags!": [ "-fno-exceptions" ],
            "cflags_cc!": [ "-fno-exceptions" ],
            "cflags_cc": [ "-std=c++17", "-fexceptions" ],
            "msvs_settings": {
                "VCCLCompilerTool": {
                    "AdditionalOptions": [
                        "/EHsc",
                        "/std:c++17"
                    ],
                    "ExceptionHandling": 1,
                    "EnablePREfast": "true"
                }
            }
        }
    ]
}
