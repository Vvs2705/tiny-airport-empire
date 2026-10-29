using System;
using UnityEditor;

namespace TA.EditorTools
{
    /// <summary>
    /// Import das malhas decimadas do Tripo (Assets/_TA/ArtLocal/Resources/Malhas/*.fbx, client/tools/malhas_ta.py). A pasta fica fora
    /// do git (a licenca do Tripo nao deixa redistribuir o modelo; GDD §10.2, decisao A): o build sai deste PC e um clone sem ela mostra as
    /// primitivas do Mundo. So a malha: sem material (o Mundo monta o Legacy Diffuse com o PNG ao lado), sem animacao, camera, luz ou
    /// colisor; compressao media e sem leitura pela CPU. Mudou alguma regra aqui: suba GetVersion para reimportar.
    /// </summary>
    public sealed class MalhaImport : AssetPostprocessor
    {
        const string Root = "Assets/_TA/ArtLocal/";

        public override uint GetVersion() => 1;

        void OnPreprocessModel()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(Root, StringComparison.OrdinalIgnoreCase)) return;
            var mi = (ModelImporter)assetImporter;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.importAnimation = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.importBlendShapes = false;
            mi.importCameras = false;
            mi.importLights = false;
            mi.addCollider = false;
            mi.isReadable = false;
            mi.meshCompression = ModelImporterMeshCompression.Medium;
            mi.importNormals = ModelImporterNormals.Import;
        }
    }
}
