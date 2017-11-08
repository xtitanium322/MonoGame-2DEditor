using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
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
        public Branch(Engine e, bool leaves, long created, int branch_with_leaves_max_variants, int branch_without_leaves_max_variants)
        {
            this.offset = Engine.generate_int_range(5, 20);  // offset along trunk height
            this.with_leaves = leaves;

            if (leaves)
                this.variant = Engine.generate_int_range(1, branch_with_leaves_max_variants); // branch variants with leaves
            else
                this.variant = Engine.generate_int_range(1, branch_without_leaves_max_variants); // branch variants without leaves

            creation_time = created;
        }
        /// <summary>
        /// Get tree branch variant for rendering
        /// </summary>
        /// <returns>tree branch variant</returns>
        public int calculate_variant()
        {
            return variant;
        }
        /// <summary>
        /// Get the offset for positioning along the trunk
        /// </summary>
        /// <returns>vertical offset in pixels</returns>
        public int get_offset()
        {
            return offset;
        }
        /// <summary>
        /// Get one of two additional variants of each branch - with leaves or without
        /// </summary>
        /// <returns>true for leaves or false for no leaves</returns>
        public bool has_leaves()
        {
            return with_leaves;
        }
        /// <summary>
        /// Get creation millisecond - for calculating the growth animation status
        /// </summary>
        /// <returns>millisecond value</returns>
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
        public Trunk(Engine e, int unique_trunk_variants)
        {
            this.left = null;
            this.right = null;
            this.variant = Engine.generate_int_range(1, unique_trunk_variants);
        }
        /// <summary>
        /// Get the left branch of this trunk
        /// </summary>
        /// <returns>branch object</returns>
        public Branch get_left()
        {
            return left;
        }
        /// <summary>
        /// Get the right branch of this trunk
        /// </summary>
        /// <returns>branch object</returns>
        public Branch get_right()
        {
            return right;
        }
        /// <summary>
        /// Create the left branch
        /// </summary>
        /// <param name="b">branch object</param>
        public void set_left(Branch b)
        {
            left = b;
        }
        /// <summary>
        /// Create the right branch
        /// </summary>
        /// <param name="b">branch object</param>
        public void set_right(Branch b)
        {
            right = b;
        }
        /// <summary>
        /// Get branch variant
        /// </summary>
        /// <returns>variant number</returns>
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
        protected int crown_variant;
        protected Vector2 base_position; // where does the tree start
        protected int base_variant;      // random bases enabled
        protected List<Trunk> trunks;    // a vertical segment (40px high - 20 px wide)
        protected int growthrate;        // number of milliseconds that must pass before next Trunk segment grows
        protected int max_trunks;        // maximum tree height
        protected long last_growth;      // millisecond
        protected Color bark_tint;       // unique tint
        protected float tint_factor;     // tint darkness factor - lower = darker

        //public abstract Vector2 get_position();                            // converted from abstract to concrete
        //public abstract void generate_branches(Engine e, int trunk_count); // removed: not every tree must generate branches
        public abstract void generate_trunk(Engine e);       
        public abstract string get_name_modifier();
        protected abstract int max_trunks_recalculation(Engine e);
        /// <summary>
        /// Get tree position as a cell address
        /// </summary>
        /// <returns>Vector2 cell position</returns>
        public Vector2 get_position()
        {
            return base_position;
        }
        /// <summary>
        /// Get the bark tint value
        /// </summary>
        /// <param name="e">engine instance</param>
        /// <returns>Color tint to apply to the entire tree (shadow effect)</returns>
        public Color get_tint_color(Engine e)
        {
            return e.adjusted_color(bark_tint, tint_factor);

        }
        /// <summary>
        /// When this tree is created based on saved data - update the defining value
        /// </summary>
        /// <param name="crown_variant">tree crown variant</param>
        /// <param name="base_variant">tree base variant</param>
        /// <param name="max_trunks">number of trunks - tree height definition</param>
        /// <param name="tint">bark color tint color</param>
        /// <param name="tint_factor">bark color tint intensity of brightness</param>
        public void set_deserialization_values(int crown_variant,int base_variant, int max_trunks, Color tint, float tint_factor)
        {        
            this.crown_variant = crown_variant;
            this.base_variant = base_variant;
            this.max_trunks = max_trunks;
            last_growth = 0;
            this.bark_tint = tint;
            this.tint_factor = tint_factor;
        }
        /// <summary>
        /// Get brightness intensity of the bark tint color
        /// </summary>
        /// <returns>float value of the factor</returns>
        public float get_tint_factor()
        {
            return tint_factor;
        }
        /// <summary>
        /// Get growth rate in milliseconds
        /// </summary>
        /// <returns>number of milliseconds between growth cycles</returns>
        public int get_growth_rate()
        {
            return growthrate;
        }
        /// <summary>
        /// Get the time of last growth
        /// </summary>
        /// <returns>time in milliseconds</returns>
        public long get_last_growth()
        {
            return last_growth;
        }
        /// <summary>
        /// Get the maximum number of trunks allowed for this tree
        /// </summary>
        /// <returns>max trunks int value</returns>
        public int get_max_trunks()
        {
            return max_trunks;
        }
        /// <summary>
        /// Get the crown variant
        /// </summary>
        /// <returns>crown variant number</returns>
        public int get_crown_variant()
        {
            return crown_variant;
        }
        /// <summary>
        /// get the base variant
        /// </summary>
        /// <returns>base variant number</returns>
        public int get_base_variant()
        {
            return base_variant;
        }
        /// <summary>
        /// Get the list of all available trunks
        /// </summary>
        /// <returns>list of Trunk objects for this tree only</returns>
        public List<Trunk> get_trunks()
        {
            return trunks;
        }
        /// <summary>
        /// Static: Preview the tree placement - if the tree can't grow to at aleast 1 trunk height - forbid the placement
        /// Only relevant for the cell based world generation
        /// </summary>
        /// <param name="e">engine instance</param>
        /// <param name="position">Cell position - base of the tree</param>
        /// <returns>true or false</returns>
        public static bool preview(Engine e, Vector2 position)
        {
            Vector2 begin = position - new Vector2(0, 2); // account for base
            int count = 0;

            while (
                (e.get_current_world().get_tile_id(e.neighbor_cell(begin, "top", 1)) == 0 && e.get_current_world().get_tile_id(e.neighbor_cell(begin, "top", 2)) == 0) // next 2 cells are air
                || (e.get_current_world().get_tile_id(e.neighbor_cell(begin, "top", 2)) == 999 && e.get_current_world().get_tile_id(e.neighbor_cell(begin, "top", 1)) == 0)
                || (e.get_current_world().get_tile_id(e.neighbor_cell(begin, "top", 1)) == 999)
                )
            {
                if (e.get_current_world().get_tile_id(e.neighbor_cell(begin, "top", 1)) == 999 || e.get_current_world().get_tile_id(e.neighbor_cell(begin, "top", 2)) == 999)
                {
                    break;
                }

                if (count >= 1)
                    break; // preview is set to allow any trees with at least 1 trunk segment

                count++;

                begin = begin - new Vector2(0, 2); // next two cells, for next trunk
            }

            if (count > 0)
                return true;
            else
                return false;
        }
    }
    // ------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// A GreenTree. All texture2D will be stored elsewhere for rendering time optimization.
    /// Trees will begin as just the base, but will grow over time.Each tree will have different growth rate.
    /// Max branches per Trunk = 4, 2 on each side
    /// </summary>
    public class GreenTree: Tree
    {
        // unique variables (not all trees have branches),e.g. palm tree has none
        private const int MAX_BRANCH_LEAVES = 7;
        private const int MAX_BRANCH_NO_LEAVES = 5;
        private const int UNIQUE_TRUNK_VARIANTS = 2;
        private const int UNIQUE_BASE_VARIANTS = 3;
        private const int UNIQUE_CROWN_VARIANTS = 3; 
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="e">engine object</param>
        /// <param name="pos">position on the map - cell address</param>
        /// <param name="growthrate">how fast should the tree grow</param>
        /// <param name="branch_growthrate">how fast should branches grow</param>
        public GreenTree(Engine e, Vector2 pos, int growthrate)
        {
            base_position = pos;
            trunks = new List<Trunk>();
            base_variant = Engine.generate_int_range(1, UNIQUE_BASE_VARIANTS);
            this.growthrate = growthrate;
            max_trunks = Engine.generate_int_range(5, 9);                               // maximum height of the tree
            crown_variant = Engine.generate_int_range(1, UNIQUE_CROWN_VARIANTS);        // what type of crown this tree has
            last_growth = 0;                                                            // set up the base value so the growth starts right away
            bark_tint = Color.White;
            tint_factor = Engine.generate_float_range(0.65f, 1f);
            
            //recalculate max trunks based on the world position of ceiling tiles
            max_trunks = max_trunks_recalculation(e);

            generate_trunk(e);
        }
        /// <summary>
        /// Based on the obstacles in the world - caves, cliffs, ceilings, etc. recalculate the maximum number of trunks before blockage
        /// </summary>
        /// <param name="e">engine instance</param>
        /// <returns>new maximum trunks value</returns>
        protected override int max_trunks_recalculation(Engine e)
        {
            Vector2 begin = base_position - new Vector2(0, 2); // account for base
            int count = 0;

            while(
                (e.get_current_world().get_tile_id(e.neighbor_cell(begin, "top", 1)) == 0 && e.get_current_world().get_tile_id(e.neighbor_cell(begin, "top", 2)) == 0 ) // next 2 cells are air
                || (e.get_current_world().get_tile_id(e.neighbor_cell(begin, "top", 2)) == 999 && e.get_current_world().get_tile_id(e.neighbor_cell(begin, "top", 1)) == 0)
                || (e.get_current_world().get_tile_id(e.neighbor_cell(begin, "top", 1)) == 999)
                )
            {
                if (e.get_current_world().get_tile_id(e.neighbor_cell(begin, "top", 1)) == 999 || e.get_current_world().get_tile_id(e.neighbor_cell(begin, "top", 2)) == 999)
                {
                    //break;
                }
                
                if (count == max_trunks)
                    break;

                count++;

                begin = begin - new Vector2(0, 2); // next two cells, for next trunk
            }

            return count;
        }
        /// <summary>
        /// Create a new trunk
        /// </summary>
        /// <param name="e">engine instance</param>
        public override void generate_trunk(Engine e)
        {
            if (trunks.Count < max_trunks && (Engine.get_current_game_millisecond() - last_growth >= growthrate))
            {
                trunks.Add(new Trunk(e, UNIQUE_TRUNK_VARIANTS));
                generate_branches(e, trunks.Count);

                last_growth = Engine.get_current_game_millisecond(); 
            }         
        }
        /// <summary>
        /// Create branches for the recently created trunk
        /// </summary>
        /// <param name="e">engine instance</param>
        /// <param name="trunk_count">number of trunks</param>
        public void generate_branches(Engine e,int trunk_count)
        {
            if (trunk_count < 2 || trunk_count == max_trunks) // first 2 trunks will have no branches
                return;
            else
            {
                if (trunk_count < 4) // first 2 trunks after that will have both branches
                {
                    int chance = Engine.generate_int_range(0, 100);
                    // both branches - low chance for leaves close to the ground
                    if (chance >= 15)
                    {
                        trunks.Last().set_left(new Branch(e, false, Engine.get_current_game_millisecond(), MAX_BRANCH_LEAVES, MAX_BRANCH_NO_LEAVES));
                    }
                    else
                    {
                        trunks.Last().set_left(new Branch(e, true, Engine.get_current_game_millisecond(), MAX_BRANCH_LEAVES, MAX_BRANCH_NO_LEAVES));
                    }
                    if (chance >= 15)
                    {
                        trunks.Last().set_right(new Branch(e, false, Engine.get_current_game_millisecond(), MAX_BRANCH_LEAVES, MAX_BRANCH_NO_LEAVES));
                    }
                    else
                    {
                        trunks.Last().set_right(new Branch(e, true, Engine.get_current_game_millisecond(), MAX_BRANCH_LEAVES, MAX_BRANCH_NO_LEAVES));
                    }
                }
                else
                {
                    // randomly left or right
                    int chance = Engine.generate_int_range(0, 100);

                    if (chance >= 50)
                    {
                        chance = Engine.generate_int_range(0, 100);
                        // uncomment for both branches - low chance for no leaves high above
                        if (chance >= 85)
                        {
                            trunks.Last().set_left(new Branch(e, false, Engine.get_current_game_millisecond(), MAX_BRANCH_LEAVES, MAX_BRANCH_NO_LEAVES));
                        }
                        else
                        {
                            trunks.Last().set_left(new Branch(e, true, Engine.get_current_game_millisecond(), MAX_BRANCH_LEAVES, MAX_BRANCH_NO_LEAVES));
                        }
                        /*if (chance >= 85)
                        {
                            trunks.Last().set_right(new Branch(e, false, e.get_current_game_millisecond(), MAX_BRANCH_LEAVES, MAX_BRANCH_NO_LEAVES));
                        }
                        else
                        {
                            trunks.Last().set_right(new Branch(e, true, e.get_current_game_millisecond(), MAX_BRANCH_LEAVES, MAX_BRANCH_NO_LEAVES));
                        }*/
                    }
                    else
                    {
                        chance = Engine.generate_int_range(0, 100);
                        // uncomment for both branches - low chance for no leaves high above
                        /*if (chance >= 85)
                        {
                            trunks.Last().set_left(new Branch(e, false, e.get_current_game_millisecond(), MAX_BRANCH_LEAVES, MAX_BRANCH_NO_LEAVES));
                        }
                        else
                        {
                            trunks.Last().set_left(new Branch(e, true, e.get_current_game_millisecond(), MAX_BRANCH_LEAVES, MAX_BRANCH_NO_LEAVES));
                        }*/
                        if (chance >= 85)
                        {
                            trunks.Last().set_right(new Branch(e, false, Engine.get_current_game_millisecond(), MAX_BRANCH_LEAVES, MAX_BRANCH_NO_LEAVES));
                        }
                        else
                        {
                            trunks.Last().set_right(new Branch(e, true, Engine.get_current_game_millisecond(), MAX_BRANCH_LEAVES, MAX_BRANCH_NO_LEAVES));
                        }
                    }
                }
            }
        }
        /// <summary>
        /// get tree name modifier - used to find the correct tree graphic elements in the collection
        /// </summary>
        /// <returns>string name modifier</returns>
        public override string get_name_modifier()
        {
            return ""; // no name modifier for green tree
        }
    }
    // ------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// A Palm Tree variant
    /// </summary>
    public class PalmTree:Tree
    {
        private string name_modifier;
        private const int UNIQUE_TRUNK_VARIANTS = 1;
        private const int UNIQUE_BASE_VARIANTS  = 1;
        private const int UNIQUE_CROWN_VARIANTS = 1; 
        /// <summary>
        /// Palm tree constructor
        /// </summary>
        /// <param name="e">engine instance</param>
        /// <param name="pos">cell address</param>
        /// <param name="growthrate">number of milliseconds between growth cycles</param>
        public PalmTree(Engine e, Vector2 pos, int growthrate):base()
        {
            base_position = pos;
            trunks = new List<Trunk>();
            base_variant = Engine.generate_int_range(1, UNIQUE_BASE_VARIANTS);          // base variants
            this.growthrate = growthrate;
            max_trunks = Engine.generate_int_range(5, 8);                               // maximum height of the tree
            crown_variant = Engine.generate_int_range(1, UNIQUE_CROWN_VARIANTS);        // what type of crown this tree has
            last_growth = 0;                                                            // set up the base value so the growth starts right away
            bark_tint = Color.White;
            tint_factor = Engine.generate_float_range(0.85f, 1f);                       // less than 1 means a darker base color 
            name_modifier = "palm";                                                     // new: name modifier will ensure a correct sprite is selected for this new tree type

            //recalculate max trunks based on the world position of ceiling tiles
            max_trunks = max_trunks_recalculation(e);

            this.generate_trunk(e);
        }
        /// <summary>
        /// Based on the obstacles in the world - caves, cliffs, ceilings, etc. recalculate the maximum number of trunks before blockage
        /// </summary>
        /// <param name="e">engine instance</param>
        /// <returns>new maximum trunks value</returns>
        protected override int max_trunks_recalculation(Engine e)
        {
            Vector2 begin = base_position - new Vector2(0, 2); // account for base
            int count = 0;

            while (
                (e.get_current_world().get_tile_id(e.neighbor_cell(begin, "top", 1)) == 0 && e.get_current_world().get_tile_id(e.neighbor_cell(begin, "top", 2)) == 0) // next 2 cells are air
                || (e.get_current_world().get_tile_id(e.neighbor_cell(begin, "top", 2)) == 999 && e.get_current_world().get_tile_id(e.neighbor_cell(begin, "top", 1)) == 0)
                || (e.get_current_world().get_tile_id(e.neighbor_cell(begin, "top", 1)) == 999)
                )
            {
                if (e.get_current_world().get_tile_id(e.neighbor_cell(begin, "top", 1)) == 999 || e.get_current_world().get_tile_id(e.neighbor_cell(begin, "top", 2)) == 999)
                {
                    break;
                }

                if (count == max_trunks)
                    break;

                count++;

                begin = begin - new Vector2(0, 2); // next two cells, for next trunk
            }

            return count;
        }
        /// <summary>
        /// Create a new trunk
        /// </summary>
        /// <param name="e">engine instance</param>
        public override void generate_trunk(Engine e)
        {
            if (trunks.Count < max_trunks && (Engine.get_current_game_millisecond() - last_growth >= growthrate))
            {
                trunks.Add(new Trunk(e, UNIQUE_TRUNK_VARIANTS));
                last_growth = Engine.get_current_game_millisecond();
            }
        }
        /// <summary>
        /// get tree name modifier - used to find the correct tree graphic elements in the collection
        /// </summary>
        /// <returns>string name modifier</returns>
        public override string get_name_modifier()
        {
            return name_modifier;
        }
    }
}
