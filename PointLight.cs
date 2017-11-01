using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
//using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace EditorEngine
{
    /// <summary>
    /// Point Light - used as a light aura around objects or a self-sufficient light source in the world.
    /// </summary>
    public class PointLight
    {
        public Color light_color;      // rgba
        public Vector2 position;       // in relation to map origin
        public Vector2 cell;           // get_cell_address address of this light
        public float sphere_radius;    // r of the sphere which this light covers
        public float light_intensity;
        public bool removable;         // true - can be deleted, false - not deleted
        public bool spriteless;        // true - no sprite will be used
        //public Texture2D sprite;       // visual representation of the light emitting object
        //public Texture2D sprite_outer; // visual representation of the light emitting objects aura - for rotation indication (faster = more bright)
        //public Texture2D light_sphere; // a texture representing light sphere around this point light

        public PointLight(Engine e, Vector2 cell, Color color, Vector2 pos, float radius, float intensity)
        {
            this.cell = cell;
            light_color = color;
            position = pos;
            sphere_radius = radius;
            light_intensity = intensity;
            //sprite       = e.get_texture("light_source_indicator"); //Game1.createSmoothCircle(20, Color.LimeGreen, 0.9f);   
            //sprite_outer = e.get_texture("light_circle");           //much quicker than Game1.createOpaqueCircle(1001, Color.White, 0.05f); // create a reach model (outline of the reach circle
        }
        // functions
        public void change_light_color(Color c)
        {
            light_color = c;
            //create_light_sphere((int)sphere_radius, light_color, light_intensity);
        }
        public void change_intensity(float val)
        {
            light_intensity = val;
        }
        public void change_range(int val)
        {
            sphere_radius = val;
        }
        // return sphere radius adjusted for pulse 
        public float active_radius()
        {
            return sphere_radius;
        }
        // Update
        public void update(int frame)
        {

        }
        public Color get_color()
        {
            return light_color;
        }
        public float get_radius()
        {
            return sphere_radius;
        }
        public float get_intensity()
        {
            return light_intensity;
        }
        // create a light sphere
        /*public void create_light_sphere(int radius, Color color, float light_intensity)
        {
            light_sphere = Game1.createSmoothCircle((int)(radius), color, light_intensity);
        }*/
    }
}
