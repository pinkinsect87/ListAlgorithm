Public Interface IFileCache
    Function GetFile(FileURI As GptwUri, FileStrmr As IFileStreamer) As System.IO.FileInfo
    Function IsInCache(FileURI As GptwUri) As Boolean
End Interface
