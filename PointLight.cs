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
        public Vector2 cell;           // cell address of this light
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
    /// <summary>
    /// A Tree. All texture2D will be stored elsewhere for rendering time optimization.
    /// Trees will begin as just the base, but will grow over time.Each tree will have different growth rate.
    /// Max branches per trunk = 4, 2 on each side
    /// </summary>
    public class Tree
    {
        private struct branch
        {
            int tree_segment_index; // starts at 1 - 1st trunk piece
            int offset;             // 0-40px from the top of the trunk segment
            bool left_side;
            int variant;            // different type of branch
        }

        private Vector2 base_position; // where does the tree start
        private int base_variant;      // random 
        private List<int> trunks;       // a vertical segment (40px high - 20 px wide)
        private List<branch> branches; // collection of branches
        private int growthrate;         // number of milliseconds that must pass before next trunk segment grows
        private int branch_growthrate; // number of milliseconds before a branch can grow
        private int max_trunks;        // maximum tree height
        
        public Tree(Vector2 pos, int max_trunk_segments, int growthrate, int branch_growthrate)
        {
            base_position = pos;
            trunks = new List<int>();
            base_variant = 1;
            branches = new List<branch>();
            this.growthrate = growthrate;
            this.branch_growthrate = branch_growthrate;
            max_trunks = max_trunk_segments;
        }

        // functions
        // cell address
        public Vector2 get_position()
        {
            return base_position;
        }

        public void generate_trunk()
        {
            if(trunks.Count < max_trunks)
            {
                trunks.Add(1); // 1st variant of the trunk
            }
        }

        public List<int> get_trunks()
        {
            return trunks;
        }
    }
}
