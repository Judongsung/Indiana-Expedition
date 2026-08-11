# Third-party notices

Indiana Expedition's original project code and documentation are distributed under the root [MIT License](LICENSE). This file records the third-party components distributed with Indiana Expedition, source adapted into the repository, build-only dependencies, and projects used only as visual or architectural reference. Third-party materials remain subject to their respective licenses.

## Components distributed with the application

### Microsoft Edge WebView2 SDK

- Package: `Microsoft.Web.WebView2` 1.0.4129.50
- Project: <https://github.com/MicrosoftEdge/WebView2Feedback>
- Package license: BSD 3-Clause
- Exact license: <https://www.nuget.org/packages/Microsoft.Web.WebView2/1.0.4129.50/License>

The application release contains the managed WebView2 assemblies and the architecture-specific `WebView2Loader.dll`. The package's unmodified `LICENSE.txt` and `NOTICE.txt` are copied to the release `licenses` directory during the build. The Microsoft Edge WebView2 Evergreen Runtime itself is not bundled; it is an external runtime prerequisite installed and serviced separately.

## Source adapted into this repository

### Microsoft Windows App Development CLI

- Project: <https://github.com/microsoft/winappCli>
- License: MIT

The project-local WGC command-line tool adapts portions of the upstream `WgcCapture` implementation for `IGraphicsCaptureItemInterop`, D3D11 frame acquisition, and GPU-to-CPU pixel readback. The adaptation removes every non-WGC fallback and does not redistribute the full CLI.

The WGC tool is a repository-local visual-testing utility and its binary is not included in the application folder produced by `scripts/build-release.ps1`. Anyone distributing the WGC binary separately must review the generated output, retain the licenses for all shipped runtime components, and update this notice as needed.

MIT License

Copyright (c) Microsoft Corporation and Contributors.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

## Build-only dependencies

The following packages are restored for compilation or source generation. Their package binaries and metadata files are not copied into the application release.

| Package | Version | License metadata | Purpose |
|---|---:|---|---|
| `Microsoft.Windows.CsWin32` | 0.3.269 | MIT | Generates the narrow Win32 and D3D11 interop surface used by the WGC test tool |
| `Microsoft.Windows.SDK.Win32Docs` | 0.1.42-alpha | Microsoft Windows SDK terms | Transitive CsWin32 documentation input |
| `Microsoft.Windows.SDK.Win32Metadata` | 69.0.7-preview | Microsoft Windows SDK terms | Transitive CsWin32 metadata input |
| `Microsoft.Windows.WDK.Win32Metadata` | 0.13.25-experimental | Microsoft Windows SDK terms | Transitive CsWin32 metadata input |
| `Microsoft.NETFramework.ReferenceAssemblies` | 1.0.3 | MIT license link in package metadata | Automatically selected reference-assembly package |
| `Microsoft.NETFramework.ReferenceAssemblies.net48` | 1.0.3 | MIT license link in package metadata | .NET Framework 4.8 compile-time reference assemblies |

The CsWin32 generator is licensed at <https://github.com/microsoft/CsWin32/blob/main/LICENSE>. The reference-assemblies package metadata points to <https://github.com/microsoft/dotnet/blob/main/LICENSE>.

## Reference-only projects

- Microsoft [WebView2Browser](https://github.com/MicrosoftEdge/WebView2Browser), BSD 3-Clause: architectural reference for WebView2 environment creation and browser-state synchronization.
- [XP.css](https://github.com/botoxparty/XP.css), MIT: cross-reference for Luna dimensions and visual structure.
- [ReactOS](https://github.com/reactos/reactos): cross-reference for separation of non-client-area responsibilities; upstream licensing varies by component and includes GPL/LGPL terms.

No source code, icons, fonts, screenshots, or other image assets from these reference-only projects are incorporated into Indiana Expedition. The Luna controls and glyphs are implemented in project-local C# drawing code. This is a code-provenance statement and does not assert ownership of, or grant permission to use, any historical product design, trade dress, or trademark.
