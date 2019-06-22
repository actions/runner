using System.IO;

namespace GitHub.Services.Content.Common
{
    public class FileStreamUtils
    {
        public static FileStream OpenFileStreamForAsync(
            string filePath, 
            FileMode mode, 
            FileAccess fileAccess, 
            FileShare fileShare, 
            FileOptions extraOptions = FileOptions.None)
        {
            try
            {
                return new FileStream(filePath, mode, fileAccess, fileShare,
                    bufferSize: 1, // disable FileStream's buggy buffering  
                    options: FileOptions.Asynchronous | FileOptions.SequentialScan | extraOptions);
            }
            catch (PathTooLongException tooLong)
            {
                throw new PathTooLongException($"Path is too long: '{filePath}'", tooLong);
            }
        }
    }
}
