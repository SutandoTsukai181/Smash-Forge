﻿using OpenTK;
using OpenTK.Graphics.OpenGL;
using SFGenericModel.Materials;
using SFGraphics.GLObjects.Shaders;
using SFGraphics.GLObjects.Textures;
using SFGraphics.GlUtils;
using System;
using System.Collections.Generic;
using SmashForge.Rendering;

namespace SmashForge.Filetypes.Models.Nuds
{
    public static class NudUniforms
    {
        private static readonly Dictionary<string, Vector4> defaultValueByProperty = new Dictionary<string, Vector4>()
        {
            { "NU_colorSamplerUV",         new Vector4(1, 1, 0, 0) },
            { "NU_colorSampler2UV",        new Vector4(1, 1, 0, 0) },
            { "NU_colorSampler3UV",        new Vector4(1, 1, 0, 0) },
            { "NU_normalSamplerAUV",       new Vector4(1, 1, 0, 0) },
            { "NU_normalSamplerBUV",       new Vector4(1, 1, 0, 0) },
            { "NU_aoMinGain",              new Vector4(0, 0, 0, 0) },
            { "NU_colorGain",              new Vector4(1, 1, 1, 1) },
            { "NU_finalColorGain",         new Vector4(1, 1, 1, 1) },
            { "NU_finalColorGain2",        new Vector4(1, 1, 1, 1) },
            { "NU_finalColorGain3",        new Vector4(1, 1, 1, 1) },
            { "NU_colorOffset",            new Vector4(0, 0, 0, 0) },
            { "NU_diffuseColor",           new Vector4(1, 1, 1, 0.5f) },
            { "NU_characterColor",         new Vector4(1, 1, 1, 1) },
            { "NU_specularColor",          new Vector4(0, 0, 0, 0) },
            { "NU_specularColorGain",      new Vector4(1, 1, 1, 1) },
            { "NU_specularParams",         new Vector4(0, 0, 0, 0) },
            { "NU_fresnelColor",           new Vector4(0, 0, 0, 0) },
            { "NU_fresnelParams",          new Vector4(0, 0, 0, 0) },
            { "NU_reflectionColor",        new Vector4(0, 0, 0, 0) },
            { "NU_reflectionParams",       new Vector4(0, 0, 0, 0) },
            { "NU_fogColor",               new Vector4(0, 0, 0, 0) },
            { "NU_fogParams",              new Vector4(0, 1, 0, 0) },
            { "NU_softLightingParams",     new Vector4(0, 0, 0, 0) },
            { "NU_customSoftLightParams",  new Vector4(0, 0, 0, 0) },
            { "NU_normalParams",           new Vector4(1, 0, 0, 0) },
            { "NU_zOffset",                new Vector4(0, 0, 0, 0) },
            { "NU_angleFadeParams",        new Vector4(0, 0, 0, 0) },
            { "NU_dualNormalScrollParams", new Vector4(0, 0, 0, 0) },
            { "NU_alphaBlendParams",       new Vector4(0, 0, 0, 0) },
            { "NU_effCombinerColor0",      new Vector4(1, 1, 1, 1) },
            { "NU_effCombinerColor1",      new Vector4(1, 1, 1, 1) },
            { "NU_effColorGain",           new Vector4(1, 1, 1, 1) },
            { "NU_effScaleUV",             new Vector4(1, 1, 0, 0) },
            { "NU_effTransUV",             new Vector4(1, 1, 0, 0) },
            { "NU_effMaxUV",               new Vector4(1, 1, 0, 0) },
            { "NU_effUniverseParam",       new Vector4(1, 0, 0, 0) },
        };

        // Default bind location for NUT textures.
        private static readonly int nutTextureUnitOffset = 0;

        public static void SetMaterialPropertyUniforms(UniformBlock uniformBlock, Shader shader, Nud.Material mat)
        {
            // Skip expensive buffer updates for redundant value updates.
            if (mat.ShouldUpdateRendering)
            {
                foreach (var property in defaultValueByProperty)
                {
                    MatPropertyShaderUniform(uniformBlock, mat, property.Key, property.Value);
                }
                mat.ShouldUpdateRendering = false;
            }

            // Always bind the uniform block because each polygon has its own uniform block.
            uniformBlock.BindBlock(shader);

            // Create some conditionals rather than using different shaders.
            var genericMaterial = new GenericMaterial();

            HasMatPropertyShaderUniform(genericMaterial, mat, "NU_softLightingParams", "hasSoftLight");
            HasMatPropertyShaderUniform(genericMaterial, mat, "NU_customSoftLightParams", "hasCustomSoftLight");
            HasMatPropertyShaderUniform(genericMaterial, mat, "NU_specularParams", "hasSpecularParams");
            HasMatPropertyShaderUniform(genericMaterial, mat, "NU_dualNormalScrollParams", "hasDualNormal");
            HasMatPropertyShaderUniform(genericMaterial, mat, "NU_normalSamplerAUV", "hasNrmSamplerAUV");
            HasMatPropertyShaderUniform(genericMaterial, mat, "NU_normalSamplerBUV", "hasNrmSamplerBUV");
            HasMatPropertyShaderUniform(genericMaterial, mat, "NU_finalColorGain", "hasFinalColorGain");
            HasMatPropertyShaderUniform(genericMaterial, mat, "NU_effUniverseParam", "hasUniverseParam");

            genericMaterial.SetShaderUniforms(shader);
        }

        private static void HasMatPropertyShaderUniform(GenericMaterial genericMaterial, Nud.Material mat, string propertyName, string uniformName)
        {
            bool hasValue = mat.HasProperty(propertyName) || mat.HasPropertyAnim(propertyName);
            if (hasValue)
                genericMaterial.AddInt(uniformName, 1);
            else
                genericMaterial.AddInt(uniformName, 0);
        }

        private static void MatPropertyShaderUniform(UniformBlock uniformBlock, Nud.Material mat, string propertyName,
            Vector4 defaultValue)
        {
            // Attempt to get the values from the material. 
            var newValues = GetValues(mat, propertyName, defaultValue);

            string uniformName = propertyName.Replace("NU_", "");

            uniformBlock.SetValue(uniformName, new Vector4(newValues[0], newValues[1], newValues[2], newValues[3]));
        }

        private static float[] GetValues(Nud.Material mat, string propertyName, Vector4 defaultValue)
        {
            float[] values = null;

            if (mat.HasPropertyAnim(propertyName))
                values = mat.GetPropertyValuesAnim(propertyName);
            else if (mat.HasProperty(propertyName))
                values = mat.GetPropertyValues(propertyName);

            if (values == null || values.Length != 4)
                values = new float[] { defaultValue.X, defaultValue.Y, defaultValue.Z, defaultValue.W };

            return values;
        }

        public static Texture GetTexture(int hash, Nud.MatTexture matTexture, int loc, Dictionary<NudEnums.DummyTexture, Texture> dummyTextures, int texType)
        {
            // Look through all loaded textures and not just the current modelcontainer.
            foreach (NUT nut in Runtime.textureContainers)
            {
                Texture texture;
                if (nut.Text != "texture.nut")
                {
                    if (texType == 1)
                        hash = 1; // not functional
                }
                if (nut.glTexByHashId.TryGetValue(hash, out texture))
                {
                    SetTextureParameters(texture, matTexture);
                    return texture;
                }
            }

            if (Enum.IsDefined(typeof(NudEnums.DummyTexture), hash))
            {
                return dummyTextures[(NudEnums.DummyTexture)hash];
            }

            return RenderTools.defaultTex;
        }

        private static void SetTextureParameters(Texture texture, Nud.MatTexture matTexture)
        {
            // Set the texture's parameters based on the material settings.
            texture.TextureWrapS = NudEnums.wrapModeByMatValue[matTexture.wrapModeS];
            texture.TextureWrapT = NudEnums.wrapModeByMatValue[matTexture.wrapModeT];
            texture.MinFilter = NudEnums.minFilterByMatValue[matTexture.minFilter];
            texture.MagFilter = NudEnums.magFilterByMatValue[matTexture.magFilter];

            if (OpenGLExtensions.IsAvailable("GL_EXT_texture_filter_anisotropic") && (texture is Texture2D))
            {
                texture.Bind();
                TextureParameterName anisotropy = (TextureParameterName)ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt;
                GL.TexParameter(TextureTarget.Texture2D, anisotropy, 0.0f);
                if (matTexture.mipDetail == 0x4 || matTexture.mipDetail == 0x6)
                    GL.TexParameter(TextureTarget.Texture2D, anisotropy, 4.0f);
            }
        }

        public static void SetTextureUniforms(Shader shader, Nud.Material mat, Dictionary<NudEnums.DummyTexture, Texture> dummyTextures)
        {
            SetHasTextureUniforms(shader, mat);
            SetRenderModeTextureUniforms(shader);

            int textureIndex = 0;

            // The type of texture can be partially determined by texture order.
            GenericMaterial textures = new GenericMaterial(nutTextureUnitOffset);
            textures.AddTexture("dif", GetTextureAndSetTexId(mat, mat.HasDiffuse, "dif", ref textureIndex, dummyTextures));
            textures.AddTexture("spheremap", GetTextureAndSetTexId(mat, mat.HasSphereMap, "spheremap", ref textureIndex, dummyTextures));
            textures.AddTexture("dif2", GetTextureAndSetTexId(mat, mat.HasDiffuse2, "dif2", ref textureIndex, dummyTextures));
            textures.AddTexture("dif3", GetTextureAndSetTexId(mat, mat.HasDiffuse3, "dif3", ref textureIndex, dummyTextures));
            textures.AddTexture("stagecube", GetTextureAndSetTexId(mat, mat.HasStageMap, "stagecube", ref textureIndex, dummyTextures));
            textures.AddTexture("cube", GetTextureAndSetTexId(mat, mat.HasCubeMap, "cube", ref textureIndex, dummyTextures));
            textures.AddTexture("ao", GetTextureAndSetTexId(mat, mat.HasAoMap, "ao", ref textureIndex, dummyTextures));
            textures.AddTexture("normalMap", GetTextureAndSetTexId(mat, mat.HasNormalMap, "normalMap", ref textureIndex, dummyTextures));
            textures.AddTexture("ramp", GetTextureAndSetTexId(mat, mat.HasRamp, "ramp", ref textureIndex, dummyTextures));
            textures.AddTexture("dummyRamp", GetTextureAndSetTexId(mat, mat.HasDummyRamp, "dummyRamp", ref textureIndex, dummyTextures));

            textures.SetShaderUniforms(shader);
        }

        public static Texture GetTextureAndSetTexId(Nud.Material mat, bool hasTex, string name, ref int textureIndex, Dictionary<NudEnums.DummyTexture, Texture> dummyTextures)
        {
            Texture texture;
            if (hasTex && textureIndex < mat.textures.Count)
            {
                // We won't know what type a texture is used for until we iterate through the textures.
                texture = GetTexture(mat.textures[textureIndex].hash, mat.textures[textureIndex], textureIndex, dummyTextures, mat.texType);
                textureIndex++;
            }
            else
            {
                if (name.ToLower().Contains("cube"))
                    texture = dummyTextures[NudEnums.DummyTexture.StageMapHigh];
                else
                    texture = RenderTools.defaultTex;
            }

            return texture;
        }

        public static void SetTextureUniformsNudMatSphere(Shader shader, Nud.Material mat, Dictionary<NudEnums.DummyTexture, Texture> dummyTextures)
        {
            SetHasTextureUniforms(shader, mat);
            SetRenderModeTextureUniforms(shader);

            // The material shader just uses predefined textures from the Resources folder.
            Nud.MatTexture diffuse = new Nud.MatTexture((int)NudEnums.DummyTexture.DummyRamp);
            Nud.MatTexture cubeMapHigh = new Nud.MatTexture((int)NudEnums.DummyTexture.StageMapHigh);

            SetHasTextureUniforms(shader, mat);
            SetRenderModeTextureUniforms(shader);

            // The type of texture can be partially determined by texture order.
            GenericMaterial textures = new GenericMaterial(nutTextureUnitOffset);
            textures.AddTexture("dif", GetSphereTexture("dif", dummyTextures));
            textures.AddTexture("spheremap", GetSphereTexture("spheremap", dummyTextures));
            textures.AddTexture("dif2", GetSphereTexture("dif2", dummyTextures));
            textures.AddTexture("dif3", GetSphereTexture("dif3", dummyTextures));
            textures.AddTexture("stagecube", GetSphereTexture("stagecube", dummyTextures));
            textures.AddTexture("cube", GetSphereTexture("cube", dummyTextures));
            textures.AddTexture("ao", GetSphereTexture("ao", dummyTextures));
            textures.AddTexture("normalMap", GetSphereTexture("normalMap", dummyTextures));
            textures.AddTexture("ramp", GetSphereTexture("ramp", dummyTextures));
            textures.AddTexture("dummyRamp", GetSphereTexture("dummyRamp", dummyTextures));

            textures.SetShaderUniforms(shader);
        }

        private static Texture GetSphereTexture(string name, Dictionary<NudEnums.DummyTexture, Texture> dummyTextures)
        {
            if (name.Contains("dif"))
                return NudMatSphereDrawing.sphereDifTex;
            else if (name.Contains("normal"))
                return NudMatSphereDrawing.sphereNrmMapTex;
            else if (name.Contains("cube"))
                return dummyTextures[NudEnums.DummyTexture.StageMapHigh];
            else
                return NudMatSphereDrawing.sphereDifTex;
        }

        public static void SetRenderModeTextureUniforms(Shader shader)
        {
            shader.SetTexture("UVTestPattern", RenderTools.uvTestPattern, 10);
            shader.SetTexture("weightRamp1", RenderTools.boneWeightGradient, 11);
            shader.SetTexture("weightRamp2", RenderTools.boneWeightGradient2, 12);
        }

        public static void SetHasTextureUniforms(Shader shader, Nud.Material mat)
        {
            shader.SetBoolToInt("hasDif", mat.HasDiffuse);
            shader.SetBoolToInt("hasDif2", mat.HasDiffuse2);
            shader.SetBoolToInt("hasDif3", mat.HasDiffuse3);
            shader.SetBoolToInt("hasStage", mat.HasStageMap);
            shader.SetBoolToInt("hasCube", mat.HasCubeMap);
            shader.SetBoolToInt("hasAo", mat.HasAoMap);
            shader.SetBoolToInt("hasNrm", mat.HasNormalMap);
            shader.SetBoolToInt("hasRamp", mat.HasRamp);
            shader.SetBoolToInt("hasDummyRamp", mat.HasDummyRamp);
            shader.SetBoolToInt("hasColorGainOffset", mat.UseColorGainOffset);
            shader.SetBoolToInt("useDiffuseBlend", mat.UseDiffuseBlend);
            shader.SetBoolToInt("hasSphereMap", mat.HasSphereMap);
            shader.SetBoolToInt("hasBayoHair", mat.HasBayoHair);
            shader.SetBoolToInt("useDifRefMask", mat.UseReflectionMask);
            shader.SetBoolToInt("softLightBrighten", mat.SoftLightBrighten);
        }
    }
}
