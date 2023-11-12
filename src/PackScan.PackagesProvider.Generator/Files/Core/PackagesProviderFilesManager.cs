using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

using PackScan.PackagesProvider.Generator.Utils;

using Path = System.IO.Path;
using Stream = System.IO.Stream;
using StreamWriter = System.IO.StreamWriter;

namespace PackScan.PackagesProvider.Generator.Files.Core;

internal sealed class PackagesProviderFilesManager : IPackagesProviderFilesManager
{
    private readonly Dictionary<string, IPackagesProviderFile> _files = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<IPackagesProviderFile> Files
    {
        get => _files.Values.ToArray();
    }

    public void RemovePhysicalFiles()
    {
        lock (_files)
        {
            string[] fileNamesToRemove = _files
                .Where(x => x.Value is PhysicalFile)
                .Select(x => x.Key)
                .ToArray();

            foreach (string fileName in fileNamesToRemove)
                _files.Remove(fileName);
        }
    }
    public bool TryGetFile(string fileName, [MaybeNullWhen(false)] out IPackagesProviderFile file)
    {
        lock (_files)
            return _files.TryGetValue(fileName, out file);
    }
    public IPackagesProviderFile AddContents(string fileName, string contents, IPackagesProviderFileModification? modification = null)
        => AddFile(fileName, () => new FileContents(this, contents, modification));
    public IPackagesProviderFile AddPhysical(string physicalFilePath, IPackagesProviderFileModification? modification = null)
        => AddFile(Path.GetFileName(physicalFilePath), () => new PhysicalFile(this, physicalFilePath, modification));
    private IPackagesProviderFile AddFile(string fileName, Func<IPackagesProviderFile> createFile)
    {
        lock (_files)
        {
            fileName = FindNotExistingFileName(fileName);

            IPackagesProviderFile file = createFile();

            _files.Add(fileName, file);

            return file;
        }
    }

    public void WriteToDirectory(string directoryPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        List<IPackagesProviderFile> files;

        lock (_files)
        {
            files = new(_files.Count);

            foreach (IPackagesProviderFile file in _files.Values)
                files.Add(file);
        }

        foreach ((string fileName, IPackagesProviderFile file) in _files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string filePath = Path.Combine(directoryPath, fileName);

            using (Stream stream = File.Create(filePath))
                file.CopyTo(stream);
        }
    }

    private string GetFileName(IPackagesProviderFile file)
    {
        lock (_files)
        {
            foreach ((string fileName, IPackagesProviderFile existingFile) in _files)
            {
                if (ReferenceEquals(existingFile, file))
                    return fileName;
            }
        }

        throw CreateUnknownException();
    }
    private void SetFileName(IPackagesProviderFile file, string newFileName)
    {
        lock (_files)
        {
            newFileName = FindNotExistingFileName(newFileName);

            string? oldFileName = null;

            foreach ((string fileName, IPackagesProviderFile existingFile) in _files)
            {
                if (ReferenceEquals(existingFile, file))
                {
                    oldFileName = fileName;
                    break;
                }
            }

            if (oldFileName is null)
                throw CreateUnknownException();

            _files.Remove(oldFileName);
            _files.Add(newFileName, file);
        }
    }

    private string FindNotExistingFileName(string fileName)
    {
        for (int i = 1; _files.ContainsKey(fileName); i++)
        {
            fileName = Path.HasExtension(fileName)
                ? Path.Combine(Path.GetFileNameWithoutExtension(fileName) + i, Path.GetExtension(fileName))
                : fileName + i;
        }

        return fileName;
    }

    private static Exception CreateUnknownException()
        => new InvalidOperationException("The file does not exist, this should not be possible");

    private abstract class FileBase : IPackagesProviderFile
    {
        private readonly PackagesProviderFilesManager _manager;

        public string Name
        {
            get => _manager.GetFileName(this);
            set => _manager.SetFileName(this, value);
        }

        protected FileBase(PackagesProviderFilesManager manager)
        {
            _manager = manager;
        }

        public abstract void CopyTo(Stream output);
        public abstract string ReadAllText();
        public abstract byte[] ReadAllBytes();
    }

    private class FileContents : FileBase
    {
        private readonly IPackagesProviderFileModification? _modification;
        private readonly string _contents;

        public FileContents(PackagesProviderFilesManager manager, string contents, IPackagesProviderFileModification? modification)
            : base(manager)
        {
            _modification = modification;
            _contents = contents;
        }

        public override void CopyTo(Stream output)
        {
            using (StreamWriter writer = new(output, Encoding.UTF8, 1024, true))
                writer.Write(ReadAllText());
        }
        public override string ReadAllText()
        {
            if (_modification?.TryModifyText(_contents, out var contents) == true)
                return contents;

            return _contents;
        }
        public override byte[] ReadAllBytes()
            => Encoding.UTF8.GetBytes(ReadAllText());
    }

    private class PhysicalFile : FileBase
    {
        private readonly IPackagesProviderFileModification? _modification;
        private readonly string _filePath;

        public PhysicalFile(PackagesProviderFilesManager manager, string filePath, IPackagesProviderFileModification? modification)
            : base(manager)
        {
            _modification = modification;
            _filePath = filePath;
        }

        public override void CopyTo(Stream output)
        {
            using (Stream fileStream = File.OpenRead(_filePath))
            {
                if (_modification is not null)
                {
                    bool success = _modification.TryModifyStream(fileStream, output);

                    if (!success)
                    {
                        fileStream.Position = 0;
                        output.Position = 0;

                        fileStream.CopyTo(output);
                    }
                }
                else
                    fileStream.CopyTo(output);
            }
        }
        public override string ReadAllText()
        {
            if (_modification is null)
                return File.ReadAllText(_filePath);

            MemoryStream memory = new();

            CopyTo(memory);

            memory.Position = 0;

            using (StreamReader reader = new(memory))
                return reader.ReadToEnd();
        }
        public override byte[] ReadAllBytes()
        {
            if (_modification is null)
                return File.ReadAllBytes(_filePath);

            MemoryStream memory = new();

            CopyTo(memory);

            return memory.ToArray();
        }
    }
}
