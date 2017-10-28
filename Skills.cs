using System;
using System.Collections.Generic;
using System.Linq;

namespace MonoGameEngine
{
    /* INCOMPLETE */
    public abstract class Skill
    {
        string name; // main name
        string tag; // dictionary key - also an ID
        short cooldown; // in ms, if 0 - no cooldown
        string type; // ranged, area , etc.
        long last_used; // ms value of last usage
        bool on_cooldown;
        bool learned;
        List<string> required; // string tags of all requirements
        int learning_cost; // cost of resources needed to learn this
        //string upgrades_to_tag;// skill that this skill upgrades to - not a prerequisite but a replacement process. new overrides this

        public Skill(string name, string tag, short cooldown, string type, int learning_cost)
        {
            this.name = name;
            this.tag = tag;
            this.cooldown = cooldown;
            this.type = type;
            last_used = 0;
            on_cooldown = false;
            learned = false;
            required = new List<string>();
            this.learning_cost = learning_cost;
        }

        /* overall updates */
        public abstract void Update();
        /*{
            // check cooldown value against last_used
            Engine e
            if(e.get_current_game_millisecond() - last_used >= cooldown)
            {
                on_cooldown = false;
            }
            else
                on_cooldown = true;		
        }*/
        public string get_name()
        {
            return name;
        }

        public string get_tag()
        {
            return tag;
        }

        public void set_cooldown(short val)
        {
            cooldown = val;
        }

        public void update_cooldown(float percent)
        {
            cooldown = (short)(cooldown * percent);
        }

        public short get_cooldown()
        {
            return cooldown;
        }

        public string get_type()
        {
            return type;
        }

        public void learn()
        {
            learned = true;
        }

        public void set_requirements(params string[] key_values) // key values = dictionary ID
        {
            foreach (string s in key_values)
            {
                required.Add(s);
            }
        }

        public List<string> get_requirements()
        {
            return required;
        }

        public bool get_learned()
        {
            return learned;
        }

        public int get_cost()
        {
            return learning_cost;
        }
    }

    // inheriting from an abstract class to build skills 
    public class Skill_Projectile : Skill
    {
        private int range;
        public Skill_Projectile(string name, string tag, short cooldown, string type, int learning_cost, int range)
            : base(name, tag, cooldown, type, learning_cost)
        {
            // construction here
            this.range = range;
        }

        public override void Update()
        {
            // do updates
            // check cooldown value against last_used
            /*Engine e
                    if(e.get_current_game_millisecond() - last_used >= cooldown)
                    {
                        on_cooldown = false;
                    }
                    else
                        on_cooldown = true;*/
        }

        public void set_range(int val)
        {
            range = val;
        }

        public void add_range(int val)
        {
            range += val;
        }

        public int get_range()
        {
            return range;
        }
    }
    // next type 
    public class Skill_Shield : Skill
    {
        private int radius;
        bool double_sided; // true = circle protection, false = only on the front side of the character, vulnerable at the back
        private int capacity; // amount of damage taken
        private int max_burst_damage; // amount of single hit protection, if exceeded - block the max and get remaining damage deducted from Player
        // animated textures and sprites are handled outside of Skill objects based on the skill tags instead in the managing class instance

        public Skill_Shield(string name, string tag, short cooldown, string type, int learning_cost, bool double_sided, int radius, int capacity, int max_burst_damage)
            : base(name, tag, cooldown, type, learning_cost)
        {
            // construction here
            this.radius = radius;
            this.capacity = capacity;
            this.double_sided = double_sided;
            this.max_burst_damage = max_burst_damage;
        }

        public override void Update()
        {
            // do updates
            // check cooldown value against last_used
            /*Engine e
                    if(e.get_current_game_millisecond() - last_used >= cooldown)
                    {
                        on_cooldown = false;
                    }
                    else
                        on_cooldown = true;*/
        }

        public void set_radius(int val)
        {
            radius = val;
        }

        public void add_radius(int val)
        {
            radius += val;
        }

        public int get_range()
        {
            return radius;
        }
    }

    public enum stat_type { strength, agility, intelligence, ranged_weapon_crit_power }; // example stats (not the real thing)

    public struct ValueTracker<T, V>
    {
        T identifier;
        V val;

        public ValueTracker(T id, V val)
            : this()
        {
            identifier = id;
            this.val = val;
        }

        public T get_identifier()
        {
            return identifier;
        }
        public V get_value()
        {
            return val;
        }
    }

    public class Skill_StatIncreasePassive : Skill
    {// tracks a dynamic sized list of Passive parameter increases by stat_type affected and a value of increase, making use of a custome generic ValueTracker structure
        private List<ValueTracker<stat_type, int>> affected;

        public Skill_StatIncreasePassive(string name, string tag, short cooldown, string type, int learning_cost, params ValueTracker<stat_type, int>[] values)
            : base(name, tag, cooldown, type, learning_cost)
        {
            // Constructor contains dynamic number of ValueTracker parameters, so multiple increases can be made for one skill
            affected = new List<ValueTracker<stat_type, int>>();

            foreach (ValueTracker<stat_type, int> v in values)
                affected.Add(v);
        }

        public override void Update()
        {
            // do updates
            // check cooldown value against last_used
            /*Engine e
            if(e.get_current_game_millisecond() - last_used >= cooldown)
            {
                on_cooldown = false;
            }
            else
                on_cooldown = true;*/
        }

        public string list_affected_stats()
        {
            string message = "";
            foreach (ValueTracker<stat_type, int> v in affected)
            {
                message += v.get_identifier().ToString() + " (" + v.get_value() + ") ";
            }
            return message;
        }
    }
    // conditional power up = depends on other skill(s) learned status or equipment in slot
    public class Skill_ConditionalPowerUp : Skill
    {
        private List<ValueTracker<string, int>> skill_req; // depends on this skill tag(s) learned status and it's skill level (currently not used)
        private string powerup_tag; // turns on hidden power up stat_increasing skills  
        bool previous_active; // last frame status 
        bool active;

        public Skill_ConditionalPowerUp(string name, string tag, short cooldown, string type, int learning_cost,
                                        string powerup_tag, params ValueTracker<string, int>[] values)
            : base(name, tag, cooldown, type, learning_cost)
        {
            this.powerup_tag = powerup_tag;
            skill_req = new List<ValueTracker<string, int>>();

            foreach (ValueTracker<string, int> v in values)
                skill_req.Add(v);

            previous_active = false;
            active = false;
        }

        public override void Update()
        {
        }

        public void Update(SkillTree s)
        {
            previous_active = active;
            // check conditions for this skill to activate
            active = check_conditions(s); // possible change

            if (active_status_changed())
            {
                // depending on current active status - add or remove power up from palyer's passive skill set
                if (active)
                {
                    // add
                }
                else
                {
                    // remove
                }
            }
        }

        public bool check_conditions(SkillTree s)
        {
            foreach (ValueTracker<string, int> v in skill_req)
            {
                if (s.is_learned(v.get_identifier()) == false)
                    return false;
            }
            return true; // all skill conditions passed
        }

        public bool active_status_changed()
        {
            return (previous_active != active); // returns true if values are different
        }
    }
    // AOE damage 
    public class Skill_AOEDamage : Skill
    {
        int aoe_range;
        //Vector2 position; // position of aoe sphere center
        bool periodic;
        int period; // ms value for repeated action
        long last_proc; // ms value of last time skill triggered
        bool proc; // allows periodic damage
        int duration; // for periodic damage - duration of the skill

        public Skill_AOEDamage(string name, string tag, short cooldown, string type, int learning_cost, int aoe_range, bool periodic, int period = 0, int duration = 0)
            : base(name, tag, cooldown, type, learning_cost)
        {
            this.aoe_range = aoe_range;
            this.periodic = periodic;
            this.period = period;
            this.duration = duration;
            last_proc = 0;
            proc = false;
        }

        public override void Update()
        {
            // do updates
            // check cooldown value against last_used
            /*Engine e
                    if(e.get_current_game_millisecond() - last_used >= cooldown)
                    {
                        on_cooldown = false;
                    }
                    else
                        on_cooldown = true;*/
        }

        public void Update(int current_ms)
        {
            if (periodic)
            {
                if (last_proc + period <= current_ms)
                {
                    proc = true;
                }
                else
                {
                    proc = false;
                }
            }
        }
        // either one time burst or periodic
        public void damage_enemies()
        {
            if (periodic)
            {
                // keep aoe zone active for duration and do periodic damage in area
            }
            else
            {
                // damage then place on cooldown and remove aoe zone
            }
        }

    }

    public class Skill_AOEEffect : Skill_AOEDamage
    {
        // List of variables for status effects applied and number of "stacks" per proc

        public Skill_AOEEffect(string name, string tag, short cooldown, string type, int learning_cost, int aoe_range, bool periodic, int period = 0, int duration = 0)
            : base(name, tag, cooldown, type, learning_cost, aoe_range, periodic, period, duration)
        {

        }
    }

    // Energy walls type of skills creates a (vertical,horizontal or both) damage barrier
    public class Skill_EnergyWalls : Skill
    {
        //Vector2 marker_position; // where the wall origin market is placed 
        bool vertical;
        bool horizontal;
        bool blocking;
        bool knockback;
        int damage_on_contact;

        public Skill_EnergyWalls(string name, string tag, short cooldown, string type, int learning_cost,
                                    bool vertical, bool horizontal, bool blocking, bool knockback, int damage)
            : base(name, tag, cooldown, type, learning_cost)
        {
            this.vertical = vertical;
            this.horizontal = horizontal;
            this.blocking = blocking;
            this.knockback = knockback;
            damage_on_contact = damage;
        }

        public override void Update()
        {
            // do updates
            // check cooldown value against last_used
            /*Engine e
            if(e.get_current_game_millisecond() - last_used >= cooldown)
            {
                on_cooldown = false;
            }
            else
                on_cooldown = true;*/
        }

        // check if wall intersects any characters, then handle Player movement restriction and damage in the SkillEngine type class
        public bool calculate_collisions()
        {
            return false;
        }
    }
    // Unlocks gear types/classes for usage
    public class Skill_GearUnlocks : Skill
    {
        string gear_class; // sub type
        string item_type;  // main type 

        public Skill_GearUnlocks(string name, string tag, short cooldown, string type, int learning_cost,
                                    string gear_class, string item_type)
            : base(name, tag, cooldown, type, learning_cost)
        {
            this.gear_class = gear_class;
            this.item_type = item_type;
        }

        public override void Update()
        {
            // do updates
            // check cooldown value against last_used
            /*Engine e
            if(e.get_current_game_millisecond() - last_used >= cooldown)
            {
                on_cooldown = false;
            }
            else
                on_cooldown = true;*/
        }

        // check player bools 
        /*public void unlock_for_player(Player p)
        {
            p.Unlock(string gear_class, string item_type);
        }*/
    }

    // ability unlocks, e.g. dual wielding 
    public class Skill_PassiveUnlocks : Skill
    {
        string skill_tag;

        public Skill_PassiveUnlocks(string name, string tag, short cooldown, string type, int learning_cost, string unlocked_skill_tag)
            : base(name, tag, cooldown, type, learning_cost)
        {
            skill_tag = unlocked_skill_tag;
        }

        public override void Update()
        {
            // do updates
            // check cooldown value against last_used
            /*Engine e
            if(e.get_current_game_millisecond() - last_used >= cooldown)
            {
                on_cooldown = false;
            }
            else
                on_cooldown = true;*/
        }

        // check player bools 
        /*public void unlock_for_player(Player p)
        {
            p.Unlock(string skill_tag); //or learn skill method from SkillTree class
        }*/
    }
    // places a marker for teleportation, delayed skill bursts, temporal and gravitational distortions
    // needs more work
    public class Skill_MarkerPlacement : Skill
    {
        //Vector2 marker_position; // where the wall origin market is placed 
        int range_of_effect;
        private ValueTracker<string, int> type_of_marker; // string = teleport, skill tag, temporal etc. int = percentage of effect or skill level or unimportant 

        public Skill_MarkerPlacement(string name, string tag, short cooldown, string type, int learning_cost,
                                        int range, string marker_type, int marker_value)
            : base(name, tag, cooldown, type, learning_cost)
        {
            range_of_effect = range;
            type_of_marker = new ValueTracker<string, int>(marker_type, marker_value);
        }

        public override void Update()
        {
            // do updates
            // check cooldown value against last_used
            /*Engine e
            if(e.get_current_game_millisecond() - last_used >= cooldown)
            {
                on_cooldown = false;
            }
            else
                on_cooldown = true;*/
        }
    }
    // handle Player assignments, locks and prerequisites and draw()
    public class SkillTree
    {
        List<Skill> skilltree;
        public SkillTree()
        {
            skilltree = new List<Skill>();
        }

        public void AddSkill(Skill s)
        {
            skilltree.Add(s);
        }

        public Skill find_skill_by_name(string key)
        {
            foreach (Skill s in skilltree)
            {
                if (s.get_name() == key)
                    return s;
            }

            return null;
        }

        public Skill find_skill_by_tag(string tag)
        {
            foreach (Skill s in skilltree)
            {
                if (s.get_tag() == tag)
                    return s;
            }

            return null;
        }

        public string get_skill_name_by_tag(string tag)
        {
            return find_skill_by_tag(tag).get_name();
        }

        public string learn_skill(string skill_tag /*, LivingCreature player*/)
        {
            Skill temp = find_skill_by_tag(skill_tag);
            // check requirements
            string message = temp.get_name() + " requirements missing: ";
            bool good = true;
            foreach (string s in temp.get_requirements())
            {
                if (find_skill_by_tag(s).get_learned() == false)
                {
                    message += get_skill_name_by_tag(s) + " ";
                    good = false;
                }
            }

            if (!good)
                return message;
            // check available "cost"
            /*if (temp.get_cost() > player.get_skill_xp_available())
                return "not enough skill xp";*/
            // learn
            temp.learn();
            return String.Concat("learned skill: ", get_skill_name_by_tag(skill_tag));
        }

        // return learned status of a skill in this tree
        public bool is_learned(string tag)
        {
            return find_skill_by_tag(tag).get_learned();
        }
    }
    /*
    public static void global_learn_skill(SkillTree s, string key)
    {
        try
        {
            Console.WriteLine(s.learn_skill(key));
        }
        catch (InvalidCastException)
        {
            Console.WriteLine("bad cast attempt"); // convert into system message or a debugger log message in developer mode
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("no object");
        }
    }*/
    /* Main program - examples of system usage */
    /*public static void Main()
    {
        // arranging objects
        SkillTree test_skills = new SkillTree();
        Skill_Projectile number1 = new Skill_Projectile("fireball", "M_FIREBALL", 500, "magic", 5, 200); // 1st tier
        Skill_Projectile number2 = new Skill_Projectile("plasma ball", "M_PLASMABALL", 5000, "magic", 25, 300); // 1st tier
        Skill_Projectile number3 = new Skill_Projectile("energy ball", "M_ENERGYBALL", 10000, "magic", 100, 400); // 2nd tier
        Skill_StatIncreasePassive number4 = new Skill_StatIncreasePassive("Warrior Spirit I", "P_STRENGTH_AGI", 0, "passive", 1000,
                                            new ValueTracker<stat_type, int>(stat_type.strength, 3),
                                            new ValueTracker<stat_type, int>(stat_type.agility, 2)); // 1st tier
        // setting requirements
        number2.set_requirements("M_FIREBALL");
        number3.set_requirements("M_FIREBALL", "M_PLASMABALL");
        // loading tests to main structure
        test_skills.AddSkill(number1);
        test_skills.AddSkill(number2);
        test_skills.AddSkill(number3);
        test_skills.AddSkill(number4);
        // safe code	 	
        Program.global_learn_skill(test_skills, "M_FIREBALL");
        Program.global_learn_skill(test_skills, "M_PLASMABALL");
        Program.global_learn_skill(test_skills, "M_ENERGYBALL");
        Program.global_learn_skill(test_skills, "P_STRENGTH_AGI");
        // examples of safe cast
        try
        {
            var temp = test_skills.find_skill_by_tag("P_STRENGTH_AGI") as Skill_StatIncreasePassive; // casting a Skill object into a child class
            Console.WriteLine("Stats affected by {0} are {1}", temp.get_name(), temp.list_affected_stats());
        }
        catch (InvalidCastException e)
        {
            Console.WriteLine("bad cast attempt : {0}", e.Message); // convert into system message or a debugger log message in developer mode
        }
        catch (NullReferenceException e)
        {
            Console.WriteLine("(\"no object\" : {0})", e.Message);
        }
    }*/
}
