using OpenTK;
using OpenTK.Graphics.OpenGL;
using SFGraphics.Cameras;
using SFGraphics.GLObjects.Shaders;
using SFGraphics.GLObjects.Textures;
using SFGraphics.Utils;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using SmashForge.Rendering;
using SmashForge.Rendering.Lights;

namespace SmashForge
{
    public class XfbinContainer : TreeNode
    {
        public Xfbin XFBIN
        {
            get
            {
                return xfbin;
            }
            set
            {
                xfbin = value;
                NUDs = new List<Nud>();
                NUTs = new List<NUT>();
                foreach (KeyValuePair<int, object> f in xfbin.files)
                {
                    if (f.Value is Nud)
                        NUDs.Add((Nud)f.Value);
                    else if (f.Value is NUT)
                        NUTs.Add((NUT)f.Value);
                }
                int n = 0;
                foreach (NUT nut in NUTs)
                {
                    //int index = xfbin.files.First(x => x.Value == nut).Key;
                    //while (xfbin.directoryCount < index + 1) index--;
                    string name = xfbin.directories[n];
                    nut.Text = name.Substring(name.LastIndexOfAny(new char[2] { '/', '\\' }) + 1);
                    foreach (Nud nud in NUDs)
                    {
                        nud.CheckTexIdErrors(nut);
                    }
                    n++;
                }
                Refresh();
                Text = xfbin.info.Name;
            }
        }

        private Xfbin xfbin;

        public List<Nud> NUDs
        {
            get
            {
                return nuds;
            }
            set
            {
                nuds = value;
                Refresh();
            }
        }
        private List<Nud> nuds;

        public List<NUT> NUTs
        {
            get
            {
                return nuts;
            }
            set
            {
                nuts = value;
                Refresh();

                //if (nuds != null)
                //    nuds.CheckTexIdErrors(nuts);
            }
        }
        private List<NUT> nuts;

        public VBN VBN
        {
            get
            {
                return vbn;
            }
            set
            {
                vbn = value;
                if (vbn == null)
                    vbn = new VBN();
                Refresh();
            }
        }
        private VBN vbn;

        public static Dictionary<string, SkelAnimation> Animations { get; set; }
        public static MovesetManager Moveset { get; set; }

        public XfbinContainer()
        {
            ImageKey = "folder";
            SelectedImageKey = "folder";
            //nuds = new Nud();
            //nuts = new NUT();
            Checked = true;
            Refresh();
        }

        public void Refresh()
        {
            Nodes.Clear();
            if (nuds != null)
            {
                foreach (Nud n in nuds)
                {
                    Nodes.Add(n);
                }
            }
            if (nuts != null)
            {
                foreach (NUT n in nuts)
                {
                    Nodes.Add(n);
                }
            }
        }

        public void Render(Camera camera, DepthTexture depthMap, Matrix4 lightMatrix, Vector2 screenDimensions, bool drawShadow = false)
        {
            if (!Checked)
                return;

            Shader shader;

            // 3DS MBN
            shader = OpenTkSharedResources.shaders["Mbn"];
            shader.UseProgram();
            SetMbnUniforms(camera, shader);

            LightColor diffuseColor = Runtime.lightSetParam.characterDiffuse.diffuseColor;
            LightColor ambientColor = Runtime.lightSetParam.characterDiffuse.ambientColor;

            if (NUDs != null && OpenTkSharedResources.shaders["Nud"].LinkStatusIsOk && OpenTkSharedResources.shaders["NudDebug"].LinkStatusIsOk)
            {
                // Choose the appropriate shader.
                if (drawShadow)
                    shader = OpenTkSharedResources.shaders["Shadow"];
                else if (Runtime.renderType != Runtime.RenderTypes.Shaded)
                    shader = OpenTkSharedResources.shaders["NudDebug"];
                else
                    shader = OpenTkSharedResources.shaders["Nud"];

                shader.UseProgram();

                // Matrices.
                Matrix4 lightMatrixRef = lightMatrix;
                shader.SetMatrix4x4("lightMatrix", ref lightMatrixRef);
                SetCameraMatrixUniforms(camera, shader);

                SetRenderSettingsUniforms(shader);
                SetLightingUniforms(shader, camera);

                shader.SetInt("renderType", (int)Runtime.renderType);
                shader.SetInt("debugOption", (int)Runtime.uvChannel);
                shader.SetBoolToInt("drawShadow", Runtime.drawModelShadow);

                shader.SetTexture("depthMap", depthMap, 14);

                SetElapsedDirectUvTime(shader);

                foreach (Nud n in NUDs)
                {
                    n.Render(VBN, camera, drawShadow, Runtime.drawNudColorIdPass);
                }
            }
        }

        private static void SetMbnUniforms(Camera camera, Shader shader)
        {
            if (Runtime.cameraLight)
            {
                shader.SetVector3("difLightDirection", Vector3.TransformNormal(new Vector3(0f, 0f, -1f), camera.MvpMatrix.Inverted()).Normalized());
            }
            else
            {
                shader.SetVector3("difLightDirection", Runtime.lightSetParam.characterDiffuse.direction);
            }
        }

        public static void SetCameraMatrixUniforms(Camera camera, Shader shader)
        {
            Matrix4 mvpMatrix = camera.MvpMatrix;
            shader.SetMatrix4x4("mvpMatrix", ref mvpMatrix);

            // Perform the calculations here to reduce render times in shader
            Matrix4 modelViewMatrix = camera.ModelViewMatrix;
            Matrix4 sphereMapMatrix = modelViewMatrix;
            sphereMapMatrix.Invert();
            sphereMapMatrix.Transpose();
            shader.SetMatrix4x4("modelViewMatrix", ref modelViewMatrix);
            shader.SetMatrix4x4("sphereMapMatrix", ref sphereMapMatrix);

            Matrix4 rotationMatrix = camera.RotationMatrix;
            shader.SetMatrix4x4("rotationMatrix", ref rotationMatrix);
        }

        private void SetElapsedDirectUvTime(Shader shader)
        {
            float elapsedSeconds = 0;
            foreach (Nud n in NUDs)
            {
                if (n.useDirectUVTime)
                {
                    elapsedSeconds = ModelViewport.directUvTimeStopWatch.ElapsedMilliseconds / 1000.0f;
                    // Should be based on XMB eventualy.
                    if (elapsedSeconds >= 100)
                        ModelViewport.directUvTimeStopWatch.Restart();
                }
                else
                    ModelViewport.directUvTimeStopWatch.Stop();
            }
            
            shader.SetFloat("elapsedTime", elapsedSeconds);
        }

        public void RenderPoints(Camera camera)
        {
            if (NUDs != null)
            {
                foreach (Nud n in NUDs)
                {
                    n.DrawPoints(camera, VBN, PrimitiveType.Triangles);
                    n.DrawPoints(camera, VBN, PrimitiveType.Points);
                }
            }
        }

        public void RenderBones()
        {
            if (VBN != null)
                RenderTools.DrawVBN(VBN);
        }

        public static void SetRenderSettingsUniforms(Shader shader)
        {
            shader.SetBoolToInt("renderStageLighting", Runtime.renderStageLighting);
            shader.SetBoolToInt("renderLighting", Runtime.renderMaterialLighting);
            shader.SetBoolToInt("renderVertColor", Runtime.renderVertColor);
            shader.SetBoolToInt("renderAlpha", Runtime.renderAlpha);
            shader.SetBoolToInt("renderDiffuse", Runtime.renderDiffuse);
            shader.SetBoolToInt("renderFresnel", Runtime.renderFresnel);
            shader.SetBoolToInt("renderSpecular", Runtime.renderSpecular);
            shader.SetBoolToInt("renderReflection", Runtime.renderReflection);

            shader.SetBoolToInt("useNormalMap", Runtime.renderNormalMap);

            shader.SetFloat("ambientIntensity", Runtime.ambIntensity);
            shader.SetFloat("diffuseIntensity", Runtime.difIntensity);
            shader.SetFloat("specularIntensity", Runtime.spcIntensity);
            shader.SetFloat("fresnelIntensity", Runtime.frsIntensity);
            shader.SetFloat("reflectionIntensity", Runtime.refIntensity);

            shader.SetFloat("zScale", Runtime.zScale);

            shader.SetBoolToInt("renderR", Runtime.renderR);
            shader.SetBoolToInt("renderG", Runtime.renderG);
            shader.SetBoolToInt("renderB", Runtime.renderB);
            shader.SetBoolToInt("renderAlpha", Runtime.renderAlpha);

            shader.SetInt("uvChannel", (int)Runtime.uvChannel);

            bool alphaOverride = Runtime.renderAlpha && !Runtime.renderR && !Runtime.renderG && !Runtime.renderB;
            shader.SetBoolToInt("alphaOverride", alphaOverride);

            shader.SetVector3("lightSetColor", 0, 0, 0);

            shader.SetInt("colorOverride", 0);

            shader.SetBoolToInt("debug1", Runtime.debug1);
            shader.SetBoolToInt("debug2", Runtime.debug2);

        }

        public static void SetLightingUniforms(Shader shader, Camera camera)
        {
            // fresnel sky/ground color for characters & stages
            ShaderTools.LightColorVector3Uniform(shader, Runtime.lightSetParam.fresnelLight.groundColor, "fresGroundColor");
            ShaderTools.LightColorVector3Uniform(shader, Runtime.lightSetParam.fresnelLight.skyColor, "fresSkyColor");
            shader.SetVector3("fresSkyDirection", Runtime.lightSetParam.fresnelLight.GetSkyDirection());
            shader.SetVector3("fresGroundDirection", Runtime.lightSetParam.fresnelLight.GetGroundDirection());

            // reflection color for characters & stages
            float refR, refG, refB = 1.0f;
            ColorUtils.HsvToRgb(Runtime.reflectionHue, Runtime.reflectionSaturation, Runtime.reflectionIntensity, out refR, out refG, out refB);
            shader.SetVector3("refLightColor", refR, refG, refB);

            // character diffuse lights
            shader.SetVector3("difLightColor", Runtime.lightSetParam.characterDiffuse.diffuseColor.R, Runtime.lightSetParam.characterDiffuse.diffuseColor.G, Runtime.lightSetParam.characterDiffuse.diffuseColor.B);
            shader.SetVector3("ambLightColor", Runtime.lightSetParam.characterDiffuse.ambientColor.R, Runtime.lightSetParam.characterDiffuse.ambientColor.G, Runtime.lightSetParam.characterDiffuse.ambientColor.B);

            shader.SetVector3("difLightColor2", Runtime.lightSetParam.characterDiffuse2.diffuseColor.R, Runtime.lightSetParam.characterDiffuse2.diffuseColor.G, Runtime.lightSetParam.characterDiffuse2.diffuseColor.B);
            shader.SetVector3("ambLightColor2", Runtime.lightSetParam.characterDiffuse2.ambientColor.R, Runtime.lightSetParam.characterDiffuse2.ambientColor.G, Runtime.lightSetParam.characterDiffuse2.ambientColor.B);

            shader.SetVector3("difLightColor3", Runtime.lightSetParam.characterDiffuse3.diffuseColor.R, Runtime.lightSetParam.characterDiffuse3.diffuseColor.G, Runtime.lightSetParam.characterDiffuse3.diffuseColor.B);
            shader.SetVector3("ambLightColor3", Runtime.lightSetParam.characterDiffuse3.ambientColor.R, Runtime.lightSetParam.characterDiffuse3.ambientColor.G, Runtime.lightSetParam.characterDiffuse3.ambientColor.B);

            // character specular light
            shader.SetVector3("specLightColor", LightTools.specularLight.diffuseColor.R, LightTools.specularLight.diffuseColor.G, LightTools.specularLight.diffuseColor.B);

            // stage fog
            shader.SetBoolToInt("renderFog", Runtime.renderFog);

            shader.SetVector3("difLight2Direction", Runtime.lightSetParam.characterDiffuse2.direction);
            shader.SetVector3("difLight3Direction", Runtime.lightSetParam.characterDiffuse2.direction);

            if (Runtime.cameraLight)
            {
                // Camera light should only affect character lighting.
                Matrix4 invertedCamera = camera.MvpMatrix.Inverted();
                Vector3 lightDirection = new Vector3(0f, 0f, -1f);
                shader.SetVector3("lightDirection", Vector3.TransformNormal(lightDirection, invertedCamera).Normalized());
                shader.SetVector3("specLightDirection", Vector3.TransformNormal(lightDirection, invertedCamera).Normalized());
                shader.SetVector3("difLightDirection", Vector3.TransformNormal(lightDirection, invertedCamera).Normalized());
            }
            else
            {
                shader.SetVector3("specLightDirection", LightTools.specularLight.direction);
                shader.SetVector3("difLightDirection", Runtime.lightSetParam.characterDiffuse.direction);
            }
        }

        public void DepthSortModels(Vector3 cameraPosition)
        {
            if (NUDs != null)
            {
                foreach (Nud n in NUDs)
                {
                    n.DepthSortMeshes(cameraPosition);
                }
            }
        }

        #region Editing Tools

        public class DuplicateKeyComparer<TKey> : IComparer<TKey> where TKey : IComparable
        {
            public int Compare(TKey x, TKey y)
            {
                int result = x.CompareTo(y);

                if (result == 0)
                    return 1;
                else
                    return result;
            }
        }

        public SortedList<double, Bone> GetBoneSelection(Ray ray)
        {
            SortedList<double, Bone> selected = new SortedList<double, Bone>(new DuplicateKeyComparer<double>());
            if (VBN != null)
            {
                Vector3 closest = Vector3.Zero;
                foreach (Bone b in VBN.bones)
                {
                    if (ray.CheckSphereHit(Vector3.TransformPosition(Vector3.Zero, b.transform), 1, out closest))
                        selected.Add(ray.Distance(closest), b);
                }
            }
            return selected;
        }

        #endregion

    }
}
