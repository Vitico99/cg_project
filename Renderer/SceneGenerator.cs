using GMath;
using Rendering;
using System;
using System.Diagnostics;
using static GMath.Gfx;
using System.Collections.Generic;

namespace Renderer
{

    /// <summary>
    /// Provides all the logic to generate a specific scene
    /// </summary>
    /// <typeparam name="V"></typeparam>
    public class SceneGenerator<V> where V : struct, INormalVertex<V>, ICoordinatesVertex<V> 
    {

        static Func<float, float, float3> EdgeGenerator(float4x4 transform)
        {
            return (u, v) =>
            {
                float alpha = u * pi / 2;
                float beta = 1 - 2 * v;
                var p = float3(cos(alpha), beta, sin(alpha));
                return mul(float4(p, 1), transform).xyz;
            };
        }


        static Func<float, float, float3> CornerGenerator(float4x4 transform)
        {
            return (u, v) =>
            {
                float alpha = u * pi / 2;
                float beta = pi / 2 - v * pi / 2;
                var p = float3(cos(alpha) * cos(beta), sin(beta), sin(alpha) * cos(beta));
                return mul(float4(p, 1), transform).xyz;
            };
        }


        static Func<float, float, float3> SquareGenerator(float4x4 transform)
        {
            return (u, v) =>
            {
                var p = float3(1 - 2 * u, 0, 1 - 2 * v);
                return mul(float4(p, 1), transform).xyz;
            };
        }


        static Func<float, float, float3> CircunferenceGenerator(float4x4 transform)
        {
            return (u, v) =>
            {
                var p = float3(0.85f * v * cos(2 * u * pi), 0, 0.85f * v * sin(u * 2 * pi));
                return mul(float4(p, 1), transform).xyz;
            };
        }


        static Func<float, float, float3> CylinderGenerator(float4x4 transform)
        {
            return (u, v) =>
            {
                float alpha = u * 2 * pi;
                float beta = 1 - 2 * v;
                var p = float3(0.85f * cos(alpha), 1f * beta, 0.85f * sin(alpha));
                return mul(float4(p, 1), transform).xyz;
            };
        }


        static Mesh<V> CylinderGenerator(int slices, int stacks, float4x4 transform)
            => Manifold<V>.Surface(slices, stacks, CylinderGenerator(transform));


        static Mesh<V> CircunferenceGenerator(int slices, int stacks, float4x4 transform)
            => Manifold<V>.Surface(slices, stacks, CircunferenceGenerator(transform));


        static Mesh<V> SmoothCubeGenerator(int slices, int stacks, float4x4 transform)
        {
            var c1 = Manifold<V>.Surface(slices, stacks, CornerGenerator(transform));

            var c2Transform = mul(Transforms.Rotate(pi / 2, float3(1, 0, 0)), Transforms.Translate(0, -2, 0));
            c2Transform = mul(c2Transform, transform);
            var c2 = Manifold<V>.Surface(slices, stacks, CornerGenerator(c2Transform));

            var c3Transform = mul(Transforms.Rotate(pi / 2, float3(0, 1, 0)), Transforms.Translate(0, 0, -2));
            c3Transform = mul(c3Transform, transform);
            var c3 = Manifold<V>.Surface(slices, stacks, CornerGenerator(c3Transform));

            var c4Transform = mul(Transforms.Rotate(pi / 2, float3(0, 0, 1)), Transforms.Translate(-2, 0, 0));
            c4Transform = mul(c4Transform, transform);
            var c4 = Manifold<V>.Surface(slices, stacks, CornerGenerator(c4Transform));

            var c5Transform = mul(Transforms.Rotate(pi, float3(0, 1, 0)), Transforms.Translate(-2, 0, -2));
            c5Transform = mul(c5Transform, transform);
            var c5 = Manifold<V>.Surface(slices, stacks, CornerGenerator(c5Transform));

            var c6Transform = mul(Transforms.Rotate(pi, float3(0, 0, 1)), Transforms.Translate(-2, -2, 0));
            c6Transform = mul(c6Transform, transform);
            var c6 = Manifold<V>.Surface(slices, stacks, CornerGenerator(c6Transform));

            var c7Transform = mul(Transforms.Rotate(3 * pi / 2, float3(0, 0, 1)), Transforms.Rotate(pi, float3(0, 1, 0)));
            c7Transform = mul(mul(c7Transform, Transforms.Translate(-2, -2, -2)), transform);
            var c7 = Manifold<V>.Surface(slices, stacks, CornerGenerator(c7Transform));

            var c8Transform = mul(Transforms.Rotate(3 * pi / 2, float3(0, 0, 1)), Transforms.Rotate(pi / 2, float3(0, 1, 0)));
            c8Transform = mul(mul(c8Transform, Transforms.Translate(0, -2, -2)), transform);
            var c8 = Manifold<V>.Surface(slices, stacks, CornerGenerator(c8Transform));


            var e1Transform = mul(Transforms.Rotate(pi / 2, float3(0, 0, 1)), Transforms.Translate(-1, 0, 0));
            e1Transform = mul(e1Transform, transform);
            var e1 = Manifold<V>.Surface(slices, stacks, EdgeGenerator(e1Transform));

            var e2Transform = mul(Transforms.Rotate(pi / 2, float3(1, 0, 0)), Transforms.Translate(0, 0, -1));
            e2Transform = mul(mul(e2Transform, Transforms.Rotate(pi / 2, float3(0, 0, 1))), transform);
            var e2 = Manifold<V>.Surface(slices, stacks, EdgeGenerator(e2Transform));

            var e3Transform = mul(Transforms.Translate(0, -1, 0), transform);
            var e3 = Manifold<V>.Surface(slices, stacks, EdgeGenerator(e3Transform));

            var e4Transform = mul(Transforms.Rotate(pi / 2, float3(0, 0, 1)), Transforms.Translate(-1, 0, 0));
            e4Transform = mul(e4Transform, Transforms.Rotate(pi / 2, float3(1, 0, 0)));
            e4Transform = mul(e4Transform, Transforms.Translate(0, -2, 0));
            e4Transform = mul(e4Transform, transform);
            var e4 = Manifold<V>.Surface(slices, stacks, EdgeGenerator(e4Transform));

            var e5Transform = mul(Transforms.Rotate(pi / 2, float3(1, 0, 0)), Transforms.Translate(0, 0, -1));
            e5Transform = mul(e5Transform, Transforms.Rotate(2 * pi, float3(0, 0, 1)));
            e5Transform = mul(e5Transform, Transforms.Translate(0, -2, 0));
            e5Transform = mul(e5Transform, transform);
            var e5 = Manifold<V>.Surface(slices, stacks, EdgeGenerator(e5Transform));

            var e6Transform = mul(Transforms.Rotate(pi / 2, float3(1, 0, 0)), Transforms.Translate(0, 0, -1));
            e6Transform = mul(e6Transform, Transforms.Rotate(pi, float3(0, 0, 1)));
            e6Transform = mul(e6Transform, Transforms.Translate(-2, 0, 0));
            e6Transform = mul(e6Transform, transform);
            var e6 = Manifold<V>.Surface(slices, stacks, EdgeGenerator(e6Transform));

            var e7Transform = mul(Transforms.Rotate(pi / 2, float3(0, 0, 1)), Transforms.Translate(-1, 0, 0));
            e7Transform = mul(e7Transform, Transforms.Rotate(3 * pi / 2, float3(1, 0, 0)));
            e7Transform = mul(e7Transform, Transforms.Translate(0, 0, -2));
            e7Transform = mul(e7Transform, transform);
            var e7 = Manifold<V>.Surface(slices, stacks, EdgeGenerator(e7Transform));

            var e8Transform = mul(Transforms.Rotate(pi / 2, float3(0, 0, 1)), Transforms.Translate(-1, 0, 0));
            e8Transform = mul(e8Transform, Transforms.Rotate(pi, float3(1, 0, 0)));
            e8Transform = mul(e8Transform, Transforms.Translate(0, -2, -2));
            e8Transform = mul(e8Transform, transform);
            var e8 = Manifold<V>.Surface(slices, stacks, EdgeGenerator(e8Transform));

            var e9Transform = mul(Transforms.Rotate(pi / 2, float3(1, 0, 0)), Transforms.Translate(0, 0, -1));
            e9Transform = mul(e9Transform, Transforms.Rotate(3 * pi / 2, float3(0, 0, 1)));
            e9Transform = mul(e9Transform, Transforms.Translate(-2, -2, 0));
            e9Transform = mul(e9Transform, transform);
            var e9 = Manifold<V>.Surface(slices, stacks, EdgeGenerator(e9Transform));

            var e10Transform = mul(Transforms.Rotate(pi / 2, float3(0, 1, 0)), Transforms.Translate(0, -1, -2));
            e10Transform = mul(e10Transform, transform);
            var e10 = Manifold<V>.Surface(slices, stacks, EdgeGenerator(e10Transform));

            var e11Transform = mul(Transforms.Rotate(3 * pi / 2, float3(0, 1, 0)), Transforms.Translate(-2, -1, 0));
            e11Transform = mul(e11Transform, transform);
            var e11 = Manifold<V>.Surface(slices, stacks, EdgeGenerator(e11Transform));

            var e12Transform = mul(Transforms.Rotate(pi, float3(0, 1, 0)), Transforms.Translate(-2, -1, -2));
            e12Transform = mul(e12Transform, transform);
            var e12 = Manifold<V>.Surface(slices, stacks, EdgeGenerator(e12Transform));

            var f1Transform = mul(Transforms.Translate(-1f, -3f, -1f), transform);
            var f1 = Manifold<V>.Surface(slices, stacks, SquareGenerator(f1Transform));

            var f2Transform = mul(Transforms.Rotate(pi / 2, float3(0, 0, 1)), Transforms.Translate(-3, -1, -1));
            f2Transform = mul(f2Transform, transform);
            var f2 = Manifold<V>.Surface(slices, stacks, SquareGenerator(f2Transform));

            var f3Transform = mul(Transforms.Rotate(pi / 2, float3(0, 0, 1)), Transforms.Translate(1, -1, -1));
            f3Transform = mul(f3Transform, transform);
            var f3 = Manifold<V>.Surface(slices, stacks, SquareGenerator(f3Transform));

            var f4Transform = mul(Transforms.Rotate(pi / 2, float3(1, 0, 0)), Transforms.Translate(-1, -1, -3));
            f4Transform = mul(f4Transform, transform);
            var f4 = Manifold<V>.Surface(slices, stacks, SquareGenerator(f4Transform));

            var f5Transform = mul(Transforms.Rotate(pi / 2, float3(1, 0, 0)), Transforms.Translate(-1, -1, 1));
            f5Transform = mul(f5Transform, transform);
            var f5 = Manifold<V>.Surface(slices, stacks, SquareGenerator(f5Transform));

            var f6Transform = Transforms.Translate(-1f, 1f, -1f);
            f6Transform = mul(f6Transform, transform);
            var f6 = Manifold<V>.Surface(slices, stacks, SquareGenerator(f6Transform));
           
            var meshes = new List<Mesh<V>> { c1, c2, c3, c4, c5, c6, c7, c8, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, f1, f2, f3, f4, f5, f6 };

            return Manifold<V>.MorphMeshes(meshes, Topology.Triangles);

        }


        static Mesh<V>[] CubeMeshes()
        {

            var c1Transform = mul(Transforms.Translate(3f, 0, 1.7f), Transforms.Scale(1.2f, 1f, 1.1f));
            var c1 = SmoothCubeGenerator(5, 5, c1Transform);

            var c2Transform = mul(Transforms.Translate(1.4f, 4f, 1.4f), Transforms.RotateRespectTo(float3(1, 0, 0), float3(0, -1, 0), pi / 7));
            var c2 = SmoothCubeGenerator(5, 5, c2Transform);

            var c3Transform = mul(Transforms.Translate(3f, 0, 5.8f), Transforms.Scale(1.2f, 1f, 1.1f));
            var c3 = SmoothCubeGenerator(5, 5, c3Transform);

            var c4Transform = mul(Transforms.Translate(5f, 4f, 2f), Transforms.RotateRespectTo(float3(1, 0, 0), float3(0, -1, 0), pi / 7));
            var c4 = SmoothCubeGenerator(5, 5, c4Transform);

            var c5Transform = mul(Transforms.Translate(3f, 4f, 6f), Transforms.RotateRespectTo(float3(1, 0, 0), float3(0, -1, 0), pi / 9));
            var c5 = SmoothCubeGenerator(5, 5, c5Transform);

            return new Mesh<V>[] { c1, c2, c3, c4, c5 }; 
           
        }

        //TODO: Implement delta function to do perfect mirror texture for ice
        //TODO: Method to make the glass transparent


        public static void CreateMeshScene(Scene<V, Material> scene)
        {
            var cubes = CubeMeshes();

            Texture2D iceTexture = new Texture2D(1, 1);
            iceTexture.Write(0, 0, float4(1, 1, 1, 1)); // plain white color

            //TODO: fix the texture reader or do a new one
            //Texture2D iceTexture = new Texture2D("C:\\Users\\Victor\\Desktop\\CG\\texture");

            foreach (var mesh in cubes)
            {
                //TODO:
                //fix the ilumination discontinuity in the cubes
                mesh.FixNormals(); // fixes the normal orientation for proper raycasting
                var c = mesh.Weld(0.001f);  // this value mitigates the discontinuity 
                c.ComputeNormals();
                scene.Add(c.AsRaycast(RaycastingMeshMode.Grid), new Material // this is the texture for the ice cubes;
                {
                    Diffuse = iceTexture,
                    Specular = float3(1, 1, 1),
                    SpecularPower = 60,
                    Glossyness = 0.2f,
                    TextureSampler = new Sampler
                    {
                        Wrap = WrapMode.Repeat,
                        MinMagFilter = Filter.Point
                    }
                }, Transforms.Identity);
            }

            var c6Transform = mul(Transforms.Translate(0.17f, 0.25f, 0.55f), Transforms.Scale(6f, 6f, 6f));
            var c6 = CylinderGenerator(25, 25, c6Transform);

            var c7Transform = mul(Transforms.Translate(0.17f, -0.75f, 0.55f), Transforms.Scale(6f, 6f, 6f));
            var c7 = CircunferenceGenerator(20, 20, c7Transform);

            var c8Transform = mul(Transforms.Translate(0.17f, -0.55f, 0.55f), Transforms.Scale(6f, 6f, 6f));
            var c8 = CircunferenceGenerator(13, 13, c8Transform);

            //TODO: Add the glass to the scene

            Texture2D tableTexture = new Texture2D(1, 1);
            tableTexture.Write(0, 0, float4(1f, 1f, 0.8f, 1));


            scene.Add(Raycasting.PlaneXZ.AttributesMap(a => new V { Position = a, Coordinates = float2(a.x, a.z), Normal = float3(0, 1, 0) }), new Material { Diffuse = tableTexture, TextureSampler = new Sampler { Wrap = WrapMode.Repeat }, Specular = float3(1, 1, 1), SpecularPower = 50, Glossyness = 0.2f },
            Transforms.Translate(0, -4f, 0)); //Table


            Texture2D wallTexture = new Texture2D(1, 1);
            wallTexture.Write(0, 0, float4(1, 1, 1, 1));

            //TODO: Fix that the wall plane does not work, idkw 
            //scene.Add(Raycasting.PlaneYZ.AttributesMap(a => new V { Position = a, Coordinates = float2(a.y, a.z), Normal = float3(-1, 0, 0) }), new Material { Diffuse = wallTexture, TextureSampler = new Sampler { Wrap = WrapMode.Repeat } },
            //mul(Transforms.Translate(-2, 0, 0), Transforms.Identity)); //Wall



        }


    }
}
