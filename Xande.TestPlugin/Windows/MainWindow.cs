using System.Numerics;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using ImGuiNET;
using Xande.Files;
using Xande.Havok;

namespace Xande.TestPlugin.Windows;

public class MainWindow : Window, IDisposable {
    private readonly FileDialogManager _fileDialogManager;
    private readonly HavokConverter    _converter;
    private readonly ModelConverter    _modelConverter;

    private const string SklbFilter = "FFXIV Skeleton{.sklb}";
    private const string PbdFilter  = "FFXIV Bone Deformer{.pbd}";
    private const string HkxFilter  = "Havok Packed File{.hkx}";
    private const string XmlFilter  = "Havok XML File{.xml}";

    public MainWindow() : base( "Xande.TestPlugin" ) {
        _fileDialogManager = new FileDialogManager();
        _converter         = new HavokConverter();

        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2( 375, 350 ),
            MaximumSize = new Vector2( 1000, 500 ),
        };

        _modelConverter = new ModelConverter( Service.DataManager.GameData, _converter );
    }

    public void Dispose() { }

    public override void Draw() {
        _fileDialogManager.Draw();

        DrawModel();
        ImGui.Separator();
        DrawParseExport();
        ImGui.Separator();
        DrawConvert();
    }

    private void DoTheThingWithTheModels( string[] models, string[] skeletons ) {
        var tempDir = Path.Combine( Path.GetTempPath(), "Xande.TestPlugin" );
        Directory.CreateDirectory( tempDir );

        var tempPath = Path.Combine( tempDir, $"model-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}" );
        Directory.CreateDirectory( tempPath );

        _modelConverter.ExportModel( tempPath, models, skeletons );
    }

    private void OpenFileDialog( string title, string filters, Action< string > callback ) {
        _fileDialogManager.OpenFileDialog( title, filters, ( result, path ) => {
            if( !result ) return;
            Service.Framework.RunOnTick( () => { callback( path ); } );
        } );
    }

    private void SaveFileDialog( string title, string filters, string defaultFileName, string defaultExtension, Action< string > callback ) {
        _fileDialogManager.SaveFileDialog( title, filters, defaultFileName, defaultExtension, ( result, path ) => {
            if( !result ) return;
            Service.Framework.RunOnTick( () => { callback( path ); } );
        } );
    }

    private void DrawModel() {
        if( ImGui.Button( "Model (full body)" ) ) {
            DoTheThingWithTheModels(
                new[] {
                    "chara/human/c0101/obj/face/f0002/model/c0101f0002_fac.mdl",
                    "chara/human/c0101/obj/body/b0001/model/c0101b0001_top.mdl",
                    "chara/human/c0101/obj/body/b0001/model/c0101b0001_glv.mdl",
                    "chara/human/c0101/obj/body/b0001/model/c0101b0001_dwn.mdl",
                    "chara/human/c0101/obj/body/b0001/model/c0101b0001_sho.mdl"
                },
                new[] {
                    "chara/human/c0101/skeleton/base/b0001/skl_c0101b0001.sklb",
                    "chara/human/c0101/skeleton/face/f0002/skl_c0101f0002.sklb"
                }
            );
        }

        ImGui.SameLine();

        if( ImGui.Button( "Model (chair)" ) ) {
            DoTheThingWithTheModels(
                new[] {
                    "bg/ffxiv/sea_s1/twn/s1ta/bgparts/s1ta_ga_char1.mdl"
                },
                new string[] { }
            );
        }

        ImGui.SameLine();

        if( ImGui.Button( "Model (grebuloff)" ) ) {
            DoTheThingWithTheModels(
                new[] {
                    "chara/monster/m0405/obj/body/b0002/model/m0405b0002.mdl"
                },
                new[] {
                    "chara/monster/m0405/skeleton/base/b0001/skl_m0405b0001.sklb"
                }
            );
        }

        ImGui.SameLine();

        if( ImGui.Button( "Model (miqote face)" ) ) {
            DoTheThingWithTheModels(
                new[] {
                    "chara/human/c0801/obj/face/f0102/model/c0801f0102_fac.mdl"
                },
                new[] {
                    "chara/human/c0801/skeleton/base/b0001/skl_c0801b0001.sklb",
                    "chara/human/c0801/skeleton/face/f0002/skl_c0801f0002.sklb"
                }
            );
        }
    }

    private void DrawParseExport() {
        if( ImGui.Button( "Parse .sklb" ) ) {
            var file = Service.DataManager.GetFile( "chara/human/c0101/skeleton/base/b0001/skl_c0101b0001.sklb" )!;
            var sklb = SklbFile.FromStream( file.Reader.BaseStream );

            var headerHex = BitConverter.ToString( sklb.RawHeader ).Replace( "-", " " );
            var hkxHex    = BitConverter.ToString( sklb.HkxData ).Replace( "-", " " );
            PluginLog.Debug( "Header (len {0}): {1}", sklb.RawHeader.Length, headerHex );
            PluginLog.Debug( "HKX data (len {0}): {1}", sklb.HkxData.Length, hkxHex );
        }

        ImGui.SameLine();

        if( ImGui.Button( "Parse .pbd" ) ) {
            var pbd = Service.DataManager.GetFile< PbdFile >( "chara/xls/boneDeformer/human.pbd" )!;

            PluginLog.Debug( "Header count: {0}", pbd.Headers.Length );
            for( var i = 0; i < pbd.Headers.Length; i++ ) {
                var header = pbd.Headers[ i ];
                PluginLog.Debug( "\tHeader {0} - ID: {1}, offset: {2}", i, header.Id, header.Offset );
            }

            PluginLog.Debug( "Deformer count: {0}", pbd.Deformers.Length );
            for( var i = 0; i < pbd.Deformers.Length; i++ ) {
                var deformer = pbd.Deformers[ i ];
                PluginLog.Debug( "\tDeformer {0} - bone count: {1}", i, deformer.BoneCount );
                for( var j = 0; j < deformer.BoneCount; j++ ) {
                    PluginLog.Debug( "\t\tBone {0} - name: {1}, deform matrix: {2}", j, deformer.BoneNames[ j ], deformer.DeformMatrices[ j ] );
                }
            }
        }

        if( ImGui.Button( "Export .sklb" ) ) {
            var file = Service.DataManager.GetFile( "chara/human/c0101/skeleton/base/b0001/skl_c0101b0001.sklb" )!;
            SaveFileDialog( "Save SKLB file", SklbFilter, "skl_c0101b0001.sklb", ".sklb", path => { File.WriteAllBytes( path, file.Data ); } );
        }

        ImGui.SameLine();

        if( ImGui.Button( "Export .pbd" ) ) {
            var file = Service.DataManager.GetFile( "chara/xls/boneDeformer/human.pbd" )!;
            SaveFileDialog( "Save PBD file", PbdFilter, "human.pbd", ".pbd", path => { File.WriteAllBytes( path, file.Data ); } );
        }
    }

    private void DrawConvert() {
        if( ImGui.Button( "SKLB->HKX" ) ) {
            OpenFileDialog( "Select SKLB file", SklbFilter, path => {
                var sklbData   = File.ReadAllBytes( path );
                var sklb       = SklbFile.FromStream( new MemoryStream( sklbData ) );
                var outputName = Path.GetFileNameWithoutExtension( path ) + ".hkx";
                SaveFileDialog( "Save HKX file", HkxFilter, outputName, ".hkx", path2 => { File.WriteAllBytes( path2, sklb.HkxData ); } );
            } );
        }

        ImGui.SameLine();

        if( ImGui.Button( "SKLB->XML" ) ) {
            OpenFileDialog( "Select SKLB file", SklbFilter, path => {
                var sklbData   = File.ReadAllBytes( path );
                var readStream = new MemoryStream( sklbData );
                var sklb       = SklbFile.FromStream( readStream );
                var xml        = _converter.HkxToXml( sklb.HkxData );
                var outputName = Path.GetFileNameWithoutExtension( path ) + ".xml";
                SaveFileDialog( "Save XML file", XmlFilter, outputName, ".xml", path2 => { File.WriteAllText( path2, xml ); } );
            } );
        }

        if( ImGui.Button( "HKX->XML" ) ) {
            OpenFileDialog( "Select HKX file", HkxFilter, path => {
                var hkx        = File.ReadAllBytes( path );
                var xml        = _converter.HkxToXml( hkx );
                var outputName = Path.GetFileNameWithoutExtension( path ) + ".xml";
                SaveFileDialog( "Save XML file", XmlFilter, outputName, ".xml", path2 => { File.WriteAllText( path2, xml ); } );
            } );
        }

        if( ImGui.Button( "XML->HKX" ) ) {
            OpenFileDialog( "Select XML file", XmlFilter, path => {
                var xml        = File.ReadAllText( path );
                var hkx        = _converter.XmlToHkx( xml );
                var outputName = Path.GetFileNameWithoutExtension( path ) + ".hkx";
                SaveFileDialog( "Save HKX file", HkxFilter, outputName, ".hkx", path2 => { File.WriteAllBytes( path2, hkx ); } );
            } );
        }

        ImGui.SameLine();

        if( ImGui.Button( "XML->SKLB" ) ) {
            OpenFileDialog( "Select XML file", XmlFilter, path => {
                OpenFileDialog( "Select original SKLB file", SklbFilter, path2 => {
                    var xml        = File.ReadAllText( path );
                    var hkx        = _converter.XmlToHkx( xml );
                    var sklbData   = File.ReadAllBytes( path2 );
                    var readStream = new MemoryStream( sklbData );
                    var sklb       = SklbFile.FromStream( readStream );

                    sklb.ReplaceHkxData( hkx );

                    var outputName = Path.GetFileNameWithoutExtension( path ) + ".sklb";
                    SaveFileDialog( "Save SKLB file", SklbFilter, outputName, ".sklb", path3 => {
                        var ms = new MemoryStream();
                        sklb.Write( ms );
                        File.WriteAllBytes( path3, ms.ToArray() );
                    } );
                } );
            } );
        }
    }
}