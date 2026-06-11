using UnityEditor;
using UnityEngine;

namespace StarForge.EditorTools
{
    public sealed class StarForgeAudioImportSettings : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            AudioImporter importer = (AudioImporter)assetImporter;
            string path = assetPath.Replace('\\', '/');

            if (path.Contains("_Project/Resources/Startup/"))
            {
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;
                importer.defaultSampleSettings = settings;
                importer.loadInBackground = true;
            }
            else if (path.Contains("_Project/Resources/Audio/"))
            {
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.6f;
                importer.defaultSampleSettings = settings;
                importer.loadInBackground = false;
                importer.forceToMono = false;
            }
        }
    }
}
