using System;
using System.IO;

namespace PhotoAlbumCreator.Common.Files;

public sealed class FileSwapper
{
    public string OriginalPath { get; }
    public string TempPath { get; }
    public string BackupPath { get; }

    private readonly DateTime _origCreation;
    private readonly DateTime _origWrite;
    private readonly DateTime _origAccess;
    private readonly FileAttributes _origAttributes;
    
    public FileSwapper(FileInfo file, string tempPath, string backupPath)
    {
        OriginalPath = file.FullName;
        TempPath = tempPath;
        BackupPath = backupPath;

        _origCreation = file.CreationTime;
        _origWrite = file.LastWriteTime;
        _origAccess = file.LastAccessTime;
        _origAttributes = file.Attributes;
    }

    public void CleanResidual()
    {
        SafeDelete(TempPath);
        SafeDelete(BackupPath);
    }

    public void Commit()
    {
        try
        {
            SafeDelete(BackupPath);

            File.Move(OriginalPath, BackupPath);
            File.Move(TempPath, OriginalPath);

            var newFile = new FileInfo(OriginalPath)
            {
                CreationTime = _origCreation,
                LastWriteTime = _origWrite,
                LastAccessTime = _origAccess
            };

            try
            {
                newFile.Attributes = _origAttributes;
            }
            catch
            {
                
            }

            SafeDelete(BackupPath);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"   Error replacing file: {ex.Message}");
            Console.ResetColor();

            SafeDelete(TempPath);
            TryRollback();
        }
    }

    private void TryRollback()
    {
        try
        {
            if (File.Exists(BackupPath) && !File.Exists(OriginalPath))
            {
                File.Move(BackupPath, OriginalPath);
            }
            else if (File.Exists(BackupPath))
            {
                SafeDelete(BackupPath);
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"   Rollback failed: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static void SafeDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            
        }
    }
}
