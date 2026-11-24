using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static WegInside.EditorTools.MotorMeshBuilder;

namespace WegInside.EditorTools
{
    static class MotorModelGenerator
    {
        internal const string PrefabPath = "Assets/Prefabs/Motor_W22.prefab";
        const string MeshFolder = "Assets/Meshes";
        const string MaterialFolder = "Assets/Materials";
        const string DataFolder = "Assets/Data";
        const float Tau = Mathf.PI * 2;

        enum Finish { Blue, DarkBlue, Steel, Laminate, Copper, Rotor, Plastic, Bearing, Dark, Plate, Ink, XRay, Highlight }

        static readonly Dictionary<Finish, Material> finishes = new();
        public static Material Transparent => finishes[Finish.XRay];
        public static Material Highlight => finishes[Finish.Highlight];

        public static GameObject Generate()
        {
            CreateMaterials();
            var root = new GameObject("Motor_W22");
            try
            {
                BuildHousing(root.transform);
                BuildStator(root.transform);
                BuildRotor(root.transform);
                BuildShaft(root.transform);
                var bearings = Child(root.transform, "Bearings");
                BuildBearing(bearings, true);
                BuildBearing(bearings, false);
                var shields = Child(root.transform, "EndShields");
                BuildShield(shields, true);
                BuildShield(shields, false);
                BuildFan(root.transform);
                BuildCover(root.transform);
                BuildTerminalBox(root.transform);
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static void CreateMaterials()
        {
            Add(Finish.Blue,"Motor_Blue_Paint","#005CA9",.34f,.42f);
            Add(Finish.DarkBlue,"Motor_Blue_Dark","#003B70",.32f,.39f);
            Add(Finish.Steel,"Steel_Brushed","#A8B2BC",.77f,.57f);
            Add(Finish.Laminate,"Stator_LaminatedSteel","#42484F",.58f,.45f);
            Add(Finish.Copper,"Copper_Winding","#B87333",.62f,.46f);
            Add(Finish.Rotor,"Rotor_Metal","#747F8A",.66f,.45f);
            Add(Finish.Plastic,"Industrial_Plastic","#24292F",.08f,.46f);
            Add(Finish.Bearing,"Bearing_Steel","#CBD1D9",.79f,.62f);
            Add(Finish.Dark,"Assembly_Dark","#212830",.4f,.37f);
            Add(Finish.Plate,"Identification_Plate","#1B2732",.21f,.37f);
            Add(Finish.Ink,"Identification_Lettering","#D9DEE5",.05f,.33f);
            Add(Finish.XRay,"Transparent_Housing","#1699D8",.15f,.3f,true);
            var highlight = Add(Finish.Highlight,"Selection_Highlight","#65C9FF",.2f,.45f);
            highlight.EnableKeyword("_EMISSION");
            highlight.SetColor("_EmissionColor", ColorFrom("#4799CF") * .45f);
            EditorUtility.SetDirty(highlight);
        }

        static Material Add(Finish key, string name, string hex, float metal, float gloss, bool transparent = false)
        {
            var material = CreateMaterial(name, hex, metal, gloss, transparent);
            finishes[key] = material;
            return material;
        }

        internal static Color ColorFrom(string hex)
        {
            ColorUtility.TryParseHtmlString(hex,out Color color);
            return color;
        }

        internal static Material CreateMaterial(string name, string hex, float metal, float gloss, bool transparent = false)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new InvalidOperationException("URP/Lit indisponível para gerar os materiais.");
            if (!material)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material,path);
            }
            material.shader = shader;
            Color color = ColorFrom(hex);
            color.a = transparent ? .18f : 1f;
            material.SetColor("_BaseColor",color);
            material.SetFloat("_Metallic",metal);
            if (!transparent) material.EnableKeyword("_EMISSION");
            material.SetFloat("_Smoothness",gloss);
            material.SetFloat("_Surface",transparent ? 1 : 0);
            material.SetFloat("_SrcBlend",transparent ? (int)BlendMode.SrcAlpha : (int)BlendMode.One);
            material.SetFloat("_DstBlend",transparent ? (int)BlendMode.OneMinusSrcAlpha : (int)BlendMode.Zero);
            material.SetFloat("_SrcBlendAlpha",(int)BlendMode.One);
            material.SetFloat("_DstBlendAlpha",transparent ? (int)BlendMode.OneMinusSrcAlpha : (int)BlendMode.Zero);
            material.SetFloat("_ZWrite",transparent ? 0 : 1);
            material.SetFloat("_Cull",(int)CullMode.Back);
            material.SetOverrideTag("RenderType",transparent ? "Transparent" : "Opaque");
            material.SetShaderPassEnabled("ShadowCaster",!transparent);
            if (transparent) material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            else material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = transparent ? (int)RenderQueue.Transparent : (int)RenderQueue.Geometry;
            EditorUtility.SetDirty(material);
            return material;
        }

        static Transform Child(Transform parent, string name, Vector3 position = default)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent,false);
            child.localPosition = position;
            return child;
        }

        static Transform Part(Transform parent, string id, string name, string group, string title, string category, string description,
            string function, string importance, Vector3 position, Vector3 offset, bool exterior = false, bool spins = false)
        {
            string path = $"{DataFolder}/{id}.asset";
            var data = AssetDatabase.LoadAssetAtPath<MotorPartData>(path);
            if (!data)
            {
                data = ScriptableObject.CreateInstance<MotorPartData>();
                AssetDatabase.CreateAsset(data,path);
            }
            data.Configure(id,group,title,category,description,function,importance);
            EditorUtility.SetDirty(data);
            Transform part = Child(parent,name,position);
            part.gameObject.AddComponent<MotorPart>().Configure(data,offset,exterior,spins);
            return part;
        }

        static Mesh Persist(string name, MotorMeshBuilder builder)
        {
            string path = $"{MeshFolder}/{name}.asset";
            var fresh = builder.Build(name);
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (!mesh)
            {
                AssetDatabase.CreateAsset(fresh,path);
                return fresh;
            }
            EditorUtility.CopySerialized(fresh,mesh);
            UnityEngine.Object.DestroyImmediate(fresh);
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        static Transform Geometry(Transform part, string name, Finish finish, MotorMeshBuilder geometry, bool collider = false)
        {
            var target = Child(part,name);
            Mesh mesh = Persist($"{part.name}_{name}",geometry);
            target.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            target.gameObject.AddComponent<MeshRenderer>().sharedMaterial = finishes[finish];
            if (collider) AddMeshCollider(target,mesh);
            return target;
        }

        static void HitMesh(Transform part, MotorMeshBuilder builder)
        {
            AddMeshCollider(Child(part,"SelectionCollider"),Persist($"{part.name}_Selection",builder));
        }

        static void AddMeshCollider(Transform target, Mesh mesh)
        {
            mesh.SetPreBakeCollisionMesh(false,true);
            EditorUtility.SetDirty(mesh);
            target.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        static void HitBox(Transform part, Vector3 center, Vector3 size)
        {
            var box = part.gameObject.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
        }

        static void BuildHousing(Transform root)
        {
            var part = Part(root,"housing","Housing","Carcaça","Carcaça","Estrutura e refrigeração",
                "Corpo fundido e aletado que envolve o conjunto eletromecânico.",
                "Protege os componentes internos e auxilia na dissipação do calor gerado durante o funcionamento do motor.",
                "A rigidez mantém o alinhamento do conjunto e as aletas aumentam a área de troca térmica.",default,default,true);
            var shell = new MotorMeshBuilder();
            shell.Lathe(default,96,new(-1.37f,1.018f),new(-1.37f,1.134f),new(-1.32f,1.183f),new(-1.17f,1.183f),
                new(-1.12f,1.105f),new(1.12f,1.105f),new(1.17f,1.183f),new(1.32f,1.183f),new(1.37f,1.134f),
                new(1.37f,1.018f),new(1.31f,1.0f),new(-1.31f,1.0f),new(-1.37f,1.018f));
            Geometry(part,"HousingBody",Finish.Blue,shell);
            var hit = new MotorMeshBuilder();
            hit.Ring(0,2.73f,1,1.115f,.025f,24);
            HitMesh(part,hit);
            var fins = new MotorMeshBuilder();
            for (int i = 0; i < 36; i++)
            {
                float a = Tau*i/36;
                float y = Mathf.Cos(a);
                if (y > .92f || y < -.95f) continue;
                float height = y < -.62f ? .125f : .195f;
                var rotation = Quaternion.AngleAxis(a * Mathf.Rad2Deg,Vector3.right);
                fins.Box(Polar(0,1.098f + height*.45f,a),new Vector3(2.26f,height,.054f),.021f,rotation);
            }
            Geometry(part,"CoolingFins",Finish.Blue,fins,true);
            var feet = new MotorMeshBuilder();
            var gussets = new MotorMeshBuilder();
            foreach (int side in new[] {-1,1})
            {
                float z = side*.91f;
                feet.Box(new Vector3(0,-1.255f,z),new Vector3(1.50f,.17f,.47f),.04f);
                foreach (int end in new[] {-1,1})
                {
                    feet.MountPad(new Vector3(end*1.00f,-1.255f,z),.17f,.255f,.085f);
                    gussets.Box(new Vector3(end*.76f,-.96f,side*.74f),new Vector3(.23f,.61f,.24f),.035f,
                        Quaternion.Euler(side * -35f,0,end * 9f));
                    gussets.Box(new Vector3(end*.42f,-1.13f,side*.91f),new Vector3(.12f,.20f,.33f),.027f);
                }
            }
            Geometry(part,"MountingFeet",Finish.Blue,feet,true);
            Geometry(part,"CastingRibs",Finish.DarkBlue,gussets,true);
            var trim = new MotorMeshBuilder();
            trim.Ring(-1.25f,.047f,1.17f,1.207f,.009f,80);
            trim.Ring(1.25f,.047f,1.17f,1.207f,.009f,80);
            Geometry(part,"RegisterEdges",Finish.DarkBlue,trim);
            BuildPlate(part);
        }

        static void BuildPlate(Transform part)
        {
            var orientation = Quaternion.Euler(-32,180,0);
            var center = new Vector3(-.30f,.693f,-1.107f);
            var plate = new MotorMeshBuilder();
            plate.Box(center,new Vector3(.78f,.284f,.042f),.015f,orientation);
            Geometry(part,"IdentificationPlate",Finish.Plate,plate,true);
            var trim = new MotorMeshBuilder();
            foreach (float y in new[] {-.116f,.116f})
                trim.Box(center+orientation*new Vector3(0,y,.023f),new Vector3(.71f,.009f,.008f),.002f,orientation);
            foreach (float x in new[] {-.336f,.336f})
                trim.Box(center+orientation*new Vector3(x,.002f,.024f),new Vector3(.019f,.019f,.009f),.003f,orientation);
            Stamp(trim,center,orientation,"W22",.275f,.073f,.023f);
            Stamp(trim,center,orientation,"DIDATICO",.282f,-.038f,.012f);
            Geometry(part,"PlateLettering",Finish.Ink,trim);
        }

        static void Stamp(MotorMeshBuilder mesh, Vector3 center, Quaternion rotation, string text, float x, float y, float step)
        {
            foreach (char c in text)
            {
                string rows = c switch
                {
                    'W' => "10001/10001/10101/10101/01010",
                    '2' => "11110/00001/01110/10000/11111",
                    'D' => "11110/10001/10001/10001/11110",
                    'I' => "111/010/010/010/111",
                    'A' => "01110/10001/11111/10001/10001",
                    'T' => "11111/00100/00100/00100/00100",
                    'C' => "01111/10000/10000/10000/01111",
                    'O' => "01110/10001/10001/10001/01110",
                    _ => "0"
                };
                var grid = rows.Split('/');
                for (int row = 0; row < grid.Length; row++)
                    for (int col = 0; col < grid[row].Length; col++)
                        if (grid[row][col] == '1')
                            mesh.Box(center+rotation*new Vector3(x-col*step,y-row*step,.024f),
                                new Vector3(step*.93f,step*.93f,.009f),.0015f,rotation);
                x -= (grid[0].Length+1)*step;
            }
        }

        static void BuildStator(Transform root)
        {
            var part = Part(root,"stator","Stator","Estator","Estator","Sistema eletromagnético",
                "Parte fixa do motor de indução trifásico. O núcleo laminado aloja enrolamentos de cobre em ranhuras.",
                "As correntes trifásicas nos enrolamentos produzem um campo magnético girante.",
                "O campo girante induz correntes no rotor, condição necessária para que ele desenvolva torque.",default,new Vector3(-3.16f,0,0));
            var core = new MotorMeshBuilder();
            core.Ring(0,1.87f,.804f,.978f,.016f,88);
            Geometry(part,"StatorCore",Finish.Laminate,core);
            var hit = new MotorMeshBuilder();
            hit.Ring(0,1.88f,.755f,.986f,.026f,32);
            HitMesh(part,hit);
            var layers = new MotorMeshBuilder();
            for (int i = 0; i < 30; i++)
            {
                float x = Mathf.Lerp(-.91f,.91f,i/29f);
                layers.Lathe(default,72,new(x-.008f,.978f),new(x-.006f,.986f),new(x+.006f,.986f),new(x+.008f,.978f));
            }
            Geometry(part,"Laminations",Finish.Rotor,layers);
            var teeth = new MotorMeshBuilder();
            var coils = new MotorMeshBuilder();
            var ties = new MotorMeshBuilder();
            const int slots = 36;
            for (int i = 0; i < slots; i++)
            {
                float a = i * Tau / slots;
                teeth.Box(Polar(0,.775f,a),new Vector3(1.868f,.079f,.078f),.012f,Quaternion.AngleAxis(a*Mathf.Rad2Deg,Vector3.right));
                float p = a + .5f*Tau/slots;
                for (int turn = 0; turn < 3; turn++)
                {
                    float r = .791f+.029f*turn;
                    float phase = p+(turn-1)*.018f;
                    coils.Tube(new[]{Polar(-.945f,r,phase),Polar(.945f,r,phase)},.016f,6);
                    if (i % 2 != 0) continue;
                    foreach (int side in new[] {-1,1})
                    {
                        var path = new List<Vector3>();
                        for (int j = 0; j <= 12; j++)
                        {
                            float t = j / 12f, arch = Mathf.Sin(Mathf.PI*t);
                            path.Add(Polar(side*(.922f+(.283f+.039f*turn)*arch),r+.077f*arch,phase+Tau/slots*t));
                        }
                        coils.Tube(path,.017f,7);
                    }
                }
                if (i % 3 == 0)
                    foreach (int side in new[] {-1,1})
                        ties.Box(Polar(side*1.177f,.905f,p),new Vector3(.075f,.037f,.066f),.01f,Quaternion.AngleAxis(p*Mathf.Rad2Deg,Vector3.right));
            }
            Geometry(part,"SlotTeeth",Finish.Laminate,teeth);
            Geometry(part,"CopperWindings",Finish.Copper,coils,true);
            Geometry(part,"WindingLacing",Finish.Dark,ties);
        }

        static void BuildRotor(Transform root)
        {
            var part = Part(root,"rotor","Rotor","Rotor","Rotor","Sistema eletromagnético",
                "Parte móvel do tipo gaiola de esquilo, com barras condutoras interligadas por anéis nas extremidades.",
                "Gira pela interação entre as correntes induzidas na gaiola e o campo magnético do estator.",
                "Converte a interação eletromagnética em torque transmitido ao eixo.",default,new Vector3(2.98f,0,0),spins:true);
            var core = new MotorMeshBuilder();
            core.Ring(0,1.82f,.232f,.677f,.028f,80);
            Geometry(part,"RotorCore",Finish.Rotor,core);
            var hit = new MotorMeshBuilder();
            hit.Ring(0,2.02f,.21f,.709f,.032f,32);
            HitMesh(part,hit);
            var layers = new MotorMeshBuilder();
            for (int i = 0; i < 28; i++)
            {
                float x = Mathf.Lerp(-.87f,.87f,i/27f);
                layers.Lathe(default,64,new(x-.009f,.677f),new(x-.006f,.684f),new(x+.006f,.684f),new(x+.009f,.677f));
            }
            Geometry(part,"RotorLaminations",Finish.Laminate,layers);
            var bars = new MotorMeshBuilder();
            for (int i = 0; i < 30; i++)
            {
                var path = new List<Vector3>();
                for (int j = 0; j <= 8; j++)
                {
                    float t = j/8f;
                    path.Add(Polar(Mathf.Lerp(-.97f,.97f,t),.682f,i*Tau/30 + .15f*t));
                }
                bars.Tube(path,.024f,7);
            }
            Geometry(part,"ConductiveBars",Finish.Copper,bars);
            var rings = new MotorMeshBuilder();
            foreach (int side in new[] {-1,1})
            {
                rings.Ring(side*.958f,.115f,.48f,.711f,.023f,80);
                rings.Ring(side*1.025f,.032f,.505f,.65f,.009f,80);
            }
            Geometry(part,"EndRings",Finish.Copper,rings);
        }

        static void BuildShaft(Transform root)
        {
            var part = Part(root,"shaft","Shaft","Eixo","Eixo","Transmissão mecânica",
                "Elemento de aço com diâmetros escalonados e um rasgo de chaveta didático na ponta de acionamento.",
                "Transmite o movimento mecânico do rotor ao equipamento acionado.",
                "Os assentos centram o eixo nos rolamentos. A chaveta representa uma forma de acoplar a carga.",default,new Vector3(.23f,1.59f,.14f),spins:true);
            var shaft = new MotorMeshBuilder();
            shaft.Lathe(default,64,new(-1.75f,.153f),new(-1.72f,.176f),new(-1.34f,.176f),new(-1.27f,.212f),
                new(-.95f,.212f),new(-.915f,.235f),new(.915f,.235f),new(.95f,.209f),new(1.27f,.209f),
                new(1.34f,.176f),new(1.56f,.176f),new(1.62f,.171f),new(2.17f,.171f),new(2.20f,.151f),new(2.20f,0));
            shaft.KeyedShaftTip();
            Geometry(part,"SteppedShaftAndKeyway",Finish.Steel,shaft);
            var hit = new MotorMeshBuilder();
            hit.Pin(-.13f,4.66f,.16f,.015f,20);
            HitMesh(part,hit);
        }

        static void BuildBearing(Transform parent, bool front)
        {
            string prefix = front ? "Front" : "Rear";
            var part = Part(parent,front ? "front-bearing" : "rear-bearing",prefix+"Bearing","Rolamentos",
                front ? "Rolamento frontal" : "Rolamento traseiro","Apoio mecânico",
                "Conjunto de anel interno, anel externo e esferas ao redor do eixo.",
                "Sustenta o eixo e reduz o atrito, permitindo uma rotação mais estável.",
                front ? "Mantém o eixo centrado no lado de acionamento, próximo ao acoplamento." :
                    "Mantém o eixo centrado no lado do ventilador e ajuda a controlar a folga radial.",
                Vector3.right*(front ? -1.43f : 1.43f),Vector3.right*(front ? -3.59f : 3.12f));
            var inner = new MotorMeshBuilder();
            inner.Ring(0,.205f,.178f,.252f,.012f,56);
            Geometry(part,"InnerRace",Finish.Bearing,inner);
            var outer = new MotorMeshBuilder();
            outer.Ring(0,.207f,.356f,.406f,.01f,56);
            Geometry(part,"OuterRace",Finish.Bearing,outer);
            var balls = new MotorMeshBuilder();
            for (int i = 0; i < 12; i++) balls.Sphere(Polar(0,.303f,i*Tau/12),.056f);
            Geometry(part,"RollingElements",Finish.Steel,balls);
            var cage = new MotorMeshBuilder();
            foreach (int direction in new[] {-1,1}) cage.Ring(direction*.066f,.027f,.276f,.335f,.007f,56);
            Geometry(part,"BallCage",Finish.Dark,cage);
            var hit = new MotorMeshBuilder();
            hit.Ring(0,.21f,.176f,.407f,.014f,24);
            HitMesh(part,hit);
        }

        static void BuildShield(Transform parent, bool front)
        {
            int direction = front ? -1 : 1;
            var part = Part(parent,front ? "front-shield" : "rear-shield",front ? "FrontEndShield" : "RearEndShield","Tampas",
                front ? "Tampa frontal" : "Tampa traseira","Estrutura",
                front ? "Fecha a carcaça do lado de acionamento e deixa passagem para a ponta do eixo." :
                    "Fecha o lado traseiro da carcaça, entre o conjunto eletromecânico e o ventilador.",
                "Fecha uma extremidade da carcaça e aloja o rolamento correspondente.",
                "O encaixe da tampa ajuda a manter o alinhamento do eixo em relação ao estator.",
                Vector3.right*(direction*1.43f),Vector3.right*(front ? -4.57f : 3.77f),true);
            Vector2 P(float x,float r) => new(direction*x,r);
            Vector2[] profile = {P(-.06f,.984f),P(-.06f,1.112f),P(-.024f,1.175f),P(.085f,1.175f),P(.148f,1.102f),
                P(.188f,.974f),P(.253f,.797f),P(.327f,.594f),P(.347f,.475f),P(.345f,.339f),P(.311f,.226f),
                P(.267f,.201f),P(.108f,.201f),P(.103f,.411f),P(-.036f,.413f),P(-.035f,.918f),P(-.06f,.984f)};
            if (front) Array.Reverse(profile);
            var shield = new MotorMeshBuilder();
            shield.Lathe(default,88,profile);
            Geometry(part,"BearingEndShieldCasting",Finish.Blue,shield);
            var hit = new MotorMeshBuilder();
            hit.Lathe(default,32,profile);
            HitMesh(part,hit);
            var lip = new MotorMeshBuilder();
            lip.Ring(direction*.269f,.078f,.207f,.34f,.017f,64);
            lip.Ring(direction*.131f,.036f,1.082f,1.119f,.006f,72);
            Geometry(part,"MachinedRegisters",Finish.DarkBlue,lip);
            var ribs = new MotorMeshBuilder();
            var nuts = new MotorMeshBuilder();
            var washers = new MotorMeshBuilder();
            for (int i = 0; i < 8; i++)
            {
                float a = (i+.5f)*Tau/8;
                var r = Quaternion.AngleAxis(a*Mathf.Rad2Deg,Vector3.right)*Quaternion.Euler(0,0,direction*18f);
                ribs.Box(Polar(direction*.279f,.75f,a),new Vector3(.083f,.64f,.103f),.027f,r);
                ribs.Pin(direction*.13f,.115f,.114f,.018f,20,Polar(0,1.055f,a));
                nuts.Pin(direction*.206f,.085f,.069f,.009f,6,Polar(0,1.055f,a));
                washers.Ring(direction*.169f,.028f,.049f,.085f,.006f,24,Polar(0,1.055f,a));
            }
            Geometry(part,"CastingRibs",Finish.Blue,ribs,true);
            var hardware = Child(part,"Fasteners");
            var heads = Geometry(part,"BoltHeads",Finish.Steel,nuts);
            var rings = Geometry(part,"Washers",Finish.Dark,washers);
            heads.SetParent(hardware,false);
            rings.SetParent(hardware,false);
        }

        static void BuildFan(Transform root)
        {
            var part = Part(root,"fan","CoolingFan","Ventilador","Ventilador","Refrigeração",
                "Ventoinha de pás curvadas montada na extremidade traseira do eixo.",
                "Promove a circulação de ar sobre a carcaça aletada para auxiliar na refrigeração do motor.",
                "Ajuda a remover o calor produzido pelos enrolamentos e por outras perdas do motor.",
                Vector3.right*1.93f,Vector3.right*4.43f,spins:true);
            var blades = new MotorMeshBuilder();
            for (int i = 0; i < 9; i++) blades.FanBlade(i*Tau/9);
            Geometry(part,"CurvedBlades",Finish.Plastic,blades,true);
            var hub = new MotorMeshBuilder();
            hub.Ring(.026f,.34f,.172f,.276f,.032f,48);
            hub.Ring(.188f,.05f,.172f,.231f,.013f,48);
            Geometry(part,"Hub",Finish.Dark,hub,true);
        }

        static void BuildCover(Transform root)
        {
            var part = Part(root,"fan-cover","FanCover","Proteção do ventilador","Proteção do ventilador","Proteção e refrigeração",
                "Cobertura metálica vazada que envolve a ventoinha no lado traseiro.",
                "Protege o ventilador contra contatos acidentais e permite a circulação de ar.",
                "Mantém a zona de rotação protegida e direciona o fluxo de ar para a carcaça.",
                Vector3.right*1.89f,Vector3.right*5.74f,true);
            var shell = new MotorMeshBuilder();
            Vector2[] profile = { new(-.43f,1.177f),new(-.43f,1.22f),new(-.392f,1.248f),new(-.23f,1.248f),new(-.19f,1.221f),
                new(.245f,1.201f),new(.339f,1.169f),new(.428f,1.081f),new(.458f,.991f),new(.413f,.98f),
                new(.378f,1.061f),new(.304f,1.145f),new(.217f,1.156f),new(-.23f,1.172f),new(-.43f,1.177f) };
            shell.Lathe(default,96,profile);
            Geometry(part,"CoverShell",Finish.Blue,shell);
            var hit = new MotorMeshBuilder();
            hit.Lathe(default,32,profile);
            HitMesh(part,hit);
            var grille = new MotorMeshBuilder();
            for (int i = 0; i < 8; i++)
            {
                float r = .19f+i*.113f;
                grille.Ring(.433f,.059f,r,r+.043f,.011f,64);
            }
            for (int i = 0; i < 10; i++)
            {
                float a = i*Tau/10;
                grille.Box(Polar(.43f,.57f,a),new Vector3(.058f,.94f,.039f),.011f,Quaternion.AngleAxis(a*Mathf.Rad2Deg,Vector3.right));
            }
            Geometry(part,"OpenGrille",Finish.DarkBlue,grille,true);
            var center = new MotorMeshBuilder();
            center.Pin(.435f,.08f,.158f,.019f,48);
            Geometry(part,"GrilleCenter",Finish.Blue,center,true);
        }

        static void BuildTerminalBox(Transform root)
        {
            var part = Part(root,"terminal-box","TerminalBox","Caixa de ligação","Caixa de ligação","Conexão elétrica",
                "Compartimento superior com prensa-cabo, tampa e seis pontos de ligação representados de forma didática.",
                "Abriga os terminais usados nas conexões elétricas do motor.",
                "Organiza e protege a interface entre a alimentação externa e os enrolamentos.",new Vector3(.24f,1.13f,0),new Vector3(-.14f,1.21f,0));
            var body = new MotorMeshBuilder();
            body.Box(new Vector3(0,.027f,0),new Vector3(.94f,.20f,.71f),.045f);
            body.Box(new Vector3(0,.14f,0),new Vector3(1.06f,.12f,.81f),.031f);
            foreach(int side in new[]{-1,1})
            {
                body.Box(new Vector3(side*.495f,.292f,0),new Vector3(.091f,.28f,.795f),.022f);
                body.Box(new Vector3(0,.292f,side*.367f),new Vector3(.955f,.28f,.079f),.018f);
            }
            Geometry(part,"BoxBody",Finish.Blue,body,true);
            var lid = new MotorMeshBuilder();
            lid.Box(new Vector3(0,.448f,0),new Vector3(1.10f,.09f,.86f),.027f);
            lid.Box(new Vector3(0,.521f,0),new Vector3(.99f,.14f,.75f),.039f);
            for (int i = 0; i < 3; i++)
                lid.Box(new Vector3(0,.592f,(i-1)*.12f),new Vector3(.36f,.022f,.041f),.008f);
            var cover = Geometry(part,"BoxCover",Finish.Blue,lid,true);
            part.GetComponent<MotorPart>().SetServiceCover(cover);
            var gasket = new MotorMeshBuilder();
            gasket.Box(new Vector3(0,.416f,0),new Vector3(1.06f,.028f,.81f),.009f);
            Geometry(part,"CoverGasket",Finish.Dark,gasket);
            var gland = new MotorMeshBuilder();
            gland.Ring(.565f,.14f,.061f,.157f,.014f,6,new Vector3(0,.257f,0));
            gland.Ring(.659f,.13f,.067f,.131f,.014f,10,new Vector3(0,.257f,0));
            gland.Ring(.737f,.06f,.066f,.101f,.012f,24,new Vector3(0,.257f,0));
            Geometry(part,"CableGland",Finish.Plastic,gland,true);
            var insulator = new MotorMeshBuilder();
            insulator.Box(new Vector3(0,.207f,0),new Vector3(.74f,.10f,.45f),.025f);
            Geometry(part,"TerminalInsulator",Finish.Plastic,insulator);
            var terminals = new MotorMeshBuilder();
            var bolts = new MotorMeshBuilder();
            var toUp = Quaternion.Euler(0,0,90);
            for (int i = 0; i < 3; i++)
                foreach (int side in new[] {-1,1})
                {
                    float x = (i-1)*.217f, z = side*.123f;
                    terminals.Box(new Vector3(x,.285f,z),new Vector3(.083f,.082f,.13f),.011f);
                    terminals.Box(new Vector3(x,.332f,z),new Vector3(.056f,.032f,.055f),.006f,toUp);
                }
            foreach (int x in new[] {-1,1})
                foreach (int z in new[] {-1,1})
                    bolts.Box(new Vector3(x*.438f,.587f,z*.312f),new Vector3(.04f,.079f,.079f),.01f,toUp);
            Geometry(part,"Terminals",Finish.Copper,terminals);
            Geometry(part,"LidFasteners",Finish.Steel,bolts).SetParent(cover,false);
        }
    }
}
