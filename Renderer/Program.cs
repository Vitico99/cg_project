using GMath;
using Rendering;
using System;
using System.Diagnostics;
using static GMath.Gfx;

namespace Renderer
{

    public struct PositionNormalCoordinate : INormalVertex<PositionNormalCoordinate>, ICoordinatesVertex<PositionNormalCoordinate>
    {
        public float3 Position { get; set; }
        public float3 Normal { get; set; }

        public float2 Coordinates { get; set; }

        public PositionNormalCoordinate Add(PositionNormalCoordinate other)
        {
            return new PositionNormalCoordinate
            {
                Position = this.Position + other.Position,
                Normal = this.Normal + other.Normal,
                Coordinates = this.Coordinates + other.Coordinates
            };
        }

        public PositionNormalCoordinate Mul(float s)
        {
            return new PositionNormalCoordinate
            {
                Position = this.Position * s,
                Normal = this.Normal * s,
                Coordinates = this.Coordinates * s
            };
        }

        public PositionNormalCoordinate Transform(float4x4 matrix)
        {
            float4 p = float4(Position, 1);
            p = mul(p, matrix);

            float4 n = float4(Normal, 0);
            n = mul(n, matrix);

            return new PositionNormalCoordinate
            {
                Position = p.xyz / p.w,
                Normal = n.xyz,
                Coordinates = Coordinates
            };
        }
    }

    public struct Material
    {
        public Texture2D Diffuse;

        public float3 Specular;
        public float SpecularPower;

        public float Glossyness;

        public Sampler TextureSampler;

        public float3 EvalBRDF(PositionNormalCoordinate surfel, float3 wout, float3 win)
        {
            float3 diffuse = Diffuse.Sample(TextureSampler, surfel.Coordinates).xyz / pi;
            float3 H = normalize(win + wout);
            float3 specular = Specular * pow(max(0, dot(H, surfel.Normal)), SpecularPower) * (SpecularPower + 2) / two_pi;
            return diffuse * (1 - Glossyness) + specular * Glossyness;
        }

    }

    class Program
    {
       
        /// <summary>
        /// Payload used to pick a color from a hit intersection
        /// </summary>
        struct MyRayPayload
        {
            public float3 Color;
        }

        /// <summary>
        /// Payload used to flag when a ray was shadowed.
        /// </summary>
        struct ShadowRayPayload
        {
            public bool Shadowed;
        }

        
        static void RaycastingMesh (Texture2D texture)
        {
            // Scene Setup
            //float3 CameraPosition = float3(25, 8f, 0f);
            float3 LightPosition = float3(30f, 10f, 5f); // add more lights the scene is a bit oscure
            //float3 LightIntensity = float3(1, 1, 1) * 100;

            float3 CameraPosition = float3(25, 8f, 0f); //mayve change the camara a bit to center the glass
            // float3(-15, 13f, 25), ligth in the right side
            // float3(-15, 13f, 0), Light in the front
            float3 LightIntensity = float3(1, 1, 1) * 3000;


            // View and projection matrices
            //float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, float3(0, 1, 0), float3(0, 1, 0));
            //float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);


            float4x4 viewMatrix = Transforms.LookAtLH(float3(25, 8f, 0f), float3(0, 0, 0), float3(0, 1, 0));

            //float4x4 viewMatrix = Transforms.LookAtLH(float3(2,4, 8), float3(0, 0, 0), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);


            //float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, float3(0, 4, 0), float3(0, 1, 0));

            //float4x4 viewMatrix = Transforms.LookAtLH(float3(2,4, 8), float3(0, 0, 0), float3(0, 1, 0));
            //float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);


            Scene<PositionNormalCoordinate, Material> scene = new Scene<PositionNormalCoordinate, Material>();
            //CreateMeshScene(scene);
            //CreateRaycastScene(scene);
            SceneGenerator<PositionNormalCoordinate>.CreateMeshScene(scene);

            // Raycaster to trace rays and check for shadow rays.
            Raytracer<ShadowRayPayload, PositionNormalCoordinate, Material> shadower = new Raytracer<ShadowRayPayload, PositionNormalCoordinate, Material>();
            shadower.OnAnyHit += delegate (IRaycastContext context, PositionNormalCoordinate attribute, Material material, ref ShadowRayPayload payload)
            {
                // If any object is found in ray-path to the light, the ray is shadowed.
                payload.Shadowed = true;
                // No neccessary to continue checking other objects
                return HitResult.Stop;
            };

            // Raycaster to trace rays and lit closest surfaces
            Raytracer<MyRayPayload, PositionNormalCoordinate, Material> raycaster = new Raytracer<MyRayPayload, PositionNormalCoordinate, Material>();
            raycaster.OnClosestHit += delegate (IRaycastContext context, PositionNormalCoordinate attribute, Material material, ref MyRayPayload payload)
            {
                // Move geometry attribute to world space
                attribute = attribute.Transform(context.FromGeometryToWorld);

                float3 V = normalize(CameraPosition - attribute.Position);
                float3 L = (LightPosition - attribute.Position);
                float d = length(L);
                L /= d; // normalize direction to light reusing distance to light

                attribute.Normal = normalize(attribute.Normal);

                float lambertFactor = max(0, dot(attribute.Normal, L));

                // Check ray to light...
                ShadowRayPayload shadow = new ShadowRayPayload();
                shadower.Trace(scene,
                    RayDescription.FromDir(attribute.Position + attribute.Normal * 0.001f, // Move an epsilon away from the surface to avoid self-shadowing 
                    L), ref shadow);

                float3 Intensity = (shadow.Shadowed ? 0.2f : 1.0f) * LightIntensity / (d * d);

                payload.Color = material.EvalBRDF(attribute, V, L) * Intensity * lambertFactor;
            };
            raycaster.OnMiss += delegate (IRaycastContext context, ref MyRayPayload payload)
            {
                payload.Color = float3(0, 0, 0); // Blue, as the sky.
            };

            /// Render all points of the screen
            for (int px = 0; px < texture.Width; px++)
                for (int py = 0; py < texture.Height; py++)
                {
                    int progress = (px * texture.Height + py);
                    if (progress % 1000 == 0)
                    {
                        Console.Write("\r" + progress * 100 / (float)(texture.Width * texture.Height) + "%            ");
                    }

                    RayDescription ray = RayDescription.FromScreen(px + 0.5f, py + 0.5f, texture.Width, texture.Height, inverse(viewMatrix), inverse(projectionMatrix), 0, 1000);

                    MyRayPayload coloring = new MyRayPayload();

                    raycaster.Trace(scene, ray, ref coloring);

                    texture.Write(px, py, float4(coloring.Color, 1));
                }
        }

        static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();

            // Texture to output the image.
            Texture2D texture = new Texture2D(512, 512);

            //SimpleRaycast(texture);
            //LitRaycast(texture);
            RaycastingMesh(texture);

            stopwatch.Stop();

            texture.Save("test.rbm");

            Console.WriteLine("Done. Rendered in " + stopwatch.ElapsedMilliseconds + " ms");
        }
    }
}
