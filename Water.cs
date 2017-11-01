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
    /// Water Generator class
    /// </summary>
    public class WaterGenerator
    {
        private Vector2 position;       // in relation to map origin
        private Vector2 cell;           // get_cell_address address of this generator
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
