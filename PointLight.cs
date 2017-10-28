﻿using System;
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
            sprite = Game1.create_colored_rectangle(new Rectangle(100, 0, 20, 20), Color.HotPink, 0.7f);
        }
        // functions
        public void change_light_color(Color c)
        {
            light_color = c;
            create_light_sphere((int)sphere_radius, light_color, light_intensity);
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
        public float get_intensity()
        {
            return light_intensity;
        }
        // create a light sphere
        public void create_light_sphere(int radius, Color color, float light_intensity)
        {
            light_sphere = Game1.createCircle((int)(radius), color, light_intensity);
        }
    }


    /// <summary>
    /// Water Generator class
    /// </summary>
    public class WaterGenerator
    {
        private Vector2 position;       // in relation to map origin
        private Vector2 cell;           // cell address of this generator
        private int intensity = 25;     // number of water units generated per cycle 
        public Texture2D sprite;        // visual representation of the object

        public WaterGenerator(Vector2 cell, Vector2 pos, int intensity)
        {
            this.cell = cell;
            position = pos;
            this.intensity = intensity;
            sprite = Game1.create_colored_rectangle(new Rectangle(100, 0, 20, 20), Color.DodgerBlue, 1.0f);
        }

        public Vector2 get_position()
        {
            return position;
        }

        public Vector2 get_cell_address()
        {
            return cell;
        }

        public int get_intensity()
        {
            return intensity;
        }
    }
}
