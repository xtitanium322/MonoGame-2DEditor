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
    /// Branch for tree Trunks
    /// </summary>
    public class Branch
    {
        int offset;             // min0-max40px from the top of the Trunk segment
        int variant;            // different type of Branch
        bool with_leaves;
        long creation_time;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="e">engine object</param>
        /// <param name="leaves">does this branch have leaves</param>
        public Branch(Engine e, bool leaves, long created)
        {
            this.offset = e.generate_int_range(5, 20);  // offset along trunk height
            this.with_leaves = leaves;

            if (leaves)
                this.variant = e.generate_int_range(1, 7); // branch variants with leaves
            else
                this.variant = e.generate_int_range(1, 5); // branch variants without leaves

            creation_time = created;
        }

        public int calculate_variant()
        {
            return variant;
        }

        public int get_offset()
        {
            return offset;
        }

        public bool has_leaves()
        {
            return with_leaves;
        }

        public long get_creation()
        {
            return creation_time;
        }
    }

    /// <summary>
    /// Trunks for a tree
    /// </summary>
    public class Trunk
    {
        Branch left;
        Branch right;
        int variant;

        /// <summary>
        /// Constructor
        /// </summary>
        public Trunk(Engine e)
        {
            this.left = null;
            this.right = null;
            this.variant = e.generate_int_range(1, 2);
        }

        public Branch get_left()
        {
            return left;
        }
        public void set_left(Branch b)
        {
            left = b;
        }
        public void set_right(Branch b)
        {
            right = b;
        }
        public Branch get_right()
        {
            return right;
        }

        public int get_variant()
        {
            return variant;
        }
    }
    // ------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Abstract base class for all types of trees present and future
    /// Any tree will have:
    /// -------------------
    /// crown, 
    /// position, 
    /// base, 
    /// trunks, 
    /// growthrate (0 or more), 
    /// max trunks limiter, 
    /// time flag for last growth, 
    /// tint color for shadows, 
    /// tint factor for random shadow intensity
    /// </summary>
    public abstract class Tree
    {
        public int crown_variant;
        public Vector2 base_position; // where does the tree start
        public int base_variant;      // random bases enabled
        public List<Trunk> trunks;    // a vertical segment (40px high - 20 px wide)
        public int growthrate;        // number of milliseconds that must pass before next Trunk segment grows
        public int max_trunks;        // maximum tree height
        public long last_growth;      // millisecond
        public Color bark_tint;       // unique tint
        public float tint_factor;     // tint darkness factor - lower = darker

        public abstract Vector2 get_position();
        public abstract void generate_trunk(Engine e);
        public abstract void generate_branches(Engine e, int trunk_count);
    }
    // ------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// A GreenTree. All texture2D will be stored elsewhere for rendering time optimization.
    /// Trees will begin as just the base, but will grow over time.Each tree will have different growth rate.
    /// Max branches per Trunk = 4, 2 on each side
    /// </summary>
    public class GreenTree: Tree
    {
        /// <summary>
        /// Construct
        /// </summary>
        /// <param name="e">engine object</param>
        /// <param name="pos">position on the map - get_cell_address address</param>
        /// <param name="growthrate">how fast should the tree grow</param>
        /// <param name="branch_growthrate">how fast should branches grow</param>
        public GreenTree(Engine e, Vector2 pos, int growthrate)
        {
            base_position = pos;
            trunks = new List<Trunk>();
            base_variant = e.generate_int_range(1,3);
            this.growthrate = growthrate;
            max_trunks = e.generate_int_range(6, 12);          // maximum height of the tree
            crown_variant = e.generate_int_range(1, 3);        // what type of crown this tree has
            last_growth = 0;                                   // set up the base value so the growth starts right away
            bark_tint = Color.White;
            tint_factor = e.generate_float_range(0.65f, 1f);

            generate_trunk(e);
        }

        // functions
        public Color get_tint_color(Engine e)
        {
           return e.adjusted_color(bark_tint,tint_factor);

        }
        public override Vector2 get_position()
        {
            return base_position;
        }
        public int get_growth_rate()
        {
            return growthrate;
        }
        public long get_last_growth()
        {
            return last_growth;
        }
        public int get_max_trunks()
        {
            return max_trunks;
        }
        public int get_crown_variant()
        {
            return crown_variant;
        }
        public int get_base_variant()
        {
            return base_variant;
        }

        public override void generate_trunk(Engine e)
        {
            if (trunks.Count < max_trunks && (e.get_current_game_millisecond()-last_growth >= growthrate))
            {
                trunks.Add(new Trunk(e));
                generate_branches(e, trunks.Count);

                last_growth = e.get_current_game_millisecond(); 
            }         
        }

        public override void generate_branches(Engine e,int trunk_count)
        {
            if (trunk_count < 2 || trunk_count == max_trunks) // first 2 trunks will have no branches
                return;
            else
            {
                if (trunk_count < 4) // first 2 trunks after that will have both branches
                {
                    int chance = e.generate_int_range(0, 100);
                    // both branches - low chance for leaves close to the ground
                    if (chance >= 15)
                    {
                        trunks.Last().set_left(new Branch(e, false,e.get_current_game_millisecond()));
                    }
                    else
                    {
                        trunks.Last().set_left(new Branch(e, true, e.get_current_game_millisecond()));
                    }
                    if (chance >= 15)
                    {
                        trunks.Last().set_right(new Branch(e, false, e.get_current_game_millisecond()));
                    }
                    else
                    {
                        trunks.Last().set_right(new Branch(e, true, e.get_current_game_millisecond()));
                    }
                }
                else
                {
                    // randomly left or right
                    int chance = e.generate_int_range(0, 100);

                    if (chance >= 50)
                    {
                        chance = e.generate_int_range(0, 100);
                        // both branches - low chance for no leaves high above
                        if (chance >= 85)
                        {
                            trunks.Last().set_left(new Branch(e, false, e.get_current_game_millisecond()));
                        }
                        else
                        {
                            trunks.Last().set_left(new Branch(e, true, e.get_current_game_millisecond()));
                        }
                        /*if (chance >= 85)
                        {
                            trunks.Last().set_right(new Branch(e, false, e.get_current_game_millisecond()));
                        }
                        else
                        {
                            trunks.Last().set_right(new Branch(e, true, e.get_current_game_millisecond()));
                        }*/
                    }
                    else
                    {
                        chance = e.generate_int_range(0, 100);
                        // both branches - low chance for no leaves high above
                        /*if (chance >= 85)
                        {
                            trunks.Last().set_left(new Branch(e, false, e.get_current_game_millisecond()));
                        }
                        else
                        {
                            trunks.Last().set_left(new Branch(e, true, e.get_current_game_millisecond()));
                        }*/
                        if (chance >= 85)
                        {
                            trunks.Last().set_right(new Branch(e, false, e.get_current_game_millisecond()));
                        }
                        else
                        {
                            trunks.Last().set_right(new Branch(e, true, e.get_current_game_millisecond()));
                        }
                    }
                }
            }
        }
        public List<Trunk> get_trunks()
        {
            return trunks;
        }
    }
}
