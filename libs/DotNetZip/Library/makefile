# ==================================================================
# Copyright (c) 2006, 2007 Microsoft Corporation.  All rights reserved.
# See the License.txt file for license information. 


_NETHOME=$(WINDIR)\Microsoft.NET\Framework\v2.0.50727
_NETSDK=c:\netsdk2.0\bin

_CS_DBG_FLAGS=/debug+
_CS_DLL_FLAGS=/t:library $(_CS_DBG_FLAGS)

_CSC=$(_NETHOME)\csc.exe


default: Zip.dll

Zip.dll: ZipFile.cs ZipDirEntry.cs ZipEntry.cs Shared.cs Crc32.cs
	$(_CSC) $(_CS_DLL_FLAGS) /out:Zip.dll ZipFile.cs ZipDirEntry.cs ZipEntry.cs Shared.cs Crc32.cs

clean:
	-del Zip.dll Zip.pdb



