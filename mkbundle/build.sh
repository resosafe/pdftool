cd ../bin/Release
#mkbundle --fetch-target mono-6.0.0-debian-9-x64
mkbundle  --deps -o pdftool pdftool.exe --cross mono-6.0.0-debian-9-x64 --library /usr/lib/libmono-native.so --config ../../mkbundle/config
