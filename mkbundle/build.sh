#!/bin/sh

msbuild ../PdfTool.sln -t:Rebuild -p:Configuration=Release
cd ../bin/Release
#mkbundle --fetch-target mono-6.0.0-debian-9-x64
mkbundle  --deps -o ../pdftool PdfTool.exe --cross mono-6.0.0-debian-9-x64 --library /usr/lib/libmono-native.so --config ../../mkbundle/config
