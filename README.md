# BB-Mono-Translation
Simple C# program to apply nameTranslation.txt onto BeeByte protected mono DLLs.

# Usage
`Beebyte-Mono-Translation.exe <path-to-dlls> <path-to-nameTranslation> <OPTIONAL-dll-name>`

- If `<OPTIONAL-dll-name>` is not specified, `Assembly-Csharp.dll` will be used as a default.

# Notes

- This program has only been tested with nameTranslation.txt that BeeByte itself makes. If you don't have that, this won't help you defeat the obfuscation.

- Currently this program will apply the original names, and delete any fake methods (if the names of the fake methods are included in nameTranslation.txt)
