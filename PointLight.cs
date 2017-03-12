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

namespace beta_windows
{
    // used as a light aura around objects
    public class PointLight
    {
        public Color light_color;      // rgba
        public Vector2 position;       // in relation to map origin
        public Vector2 cell;           // cell address of this light
        public float sphere_radius;    // r of the sphere which this light covers
        public float light_intensity;
        public bool removable;         // true - can be deleted, false - not deleted
        public bool spriteless;        // true - no sprite will be used
        public Texture2D sprite;       // visual representation of the light emitting object
        public Texture2D light_sphere; // a texture representing light sphere around this point light

        public PointLight(Vector2 cell, Color color, Vector2 pos, float radius, float intensity)
        {
            this.cell = cell;
            light_color = color;
            position = pos;
            sphere_radius = radius;
            light_intensity = intensity;
        }
        // functions
        // return sphere radius adjusted for pulse 
        public float active_radius()
        {
            return sphere_radius;
        }
        // Update
        public void update(int frame)
        {

        }
        // create a light sphere
        public void create_light_sphere(int radius, Color color, float light_intensity)
        {
            light_sphere = Game1.createCircle((int)(radius), color, light_intensity);
        }
    }
}
