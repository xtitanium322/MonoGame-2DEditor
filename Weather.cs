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
    /// A cloud representation
    /// Features: fading in and out, rain state, limited lifetime and movement. 
    /// </summary>
    public class Cloud : IParticleCreating
    {
        // interface required properties
        public List<Emitter> emitters
        {
            get { return _emitters;}
            set { _emitters = value;}
        }

        private List<Emitter> _emitters;                // real emitters list to power the property above
        private const int UNIQUE_CLOUD_VARIANTS = 9;    // number of cloud variant sprites available
        private const long FADEOUT_TIME = 1000;         // 1 second to be removed from existence     
        private Vector2 position;                       // position in pixels in relation to map origin
        private long creation_time;                     // ms when cloud was created
        private long lifetime;                          // how long will this cloud exist (rain status overrides this until rain ends) 
        private long last_update;                       // ms of the last position update
        private long scheduled_for_fadeout;             // ms whencloud fadeout period began - after FADEOUT_TIME it will be removed from the List<> and a new one will be created
        private int cloud_variant;                      // sprite version of the cloud 
        private bool rain;                              // true = read to rain, false = normal cloud
        private int rain_capacity;                      // number of rain particles that will generate when rain starts
        private bool remove;                            // this flag indicates if it's time to delete this cloud
        private float opacity_factor;                   // how transparent is this cloud?
        private int rain_start_trigger;                 // this number will start rain for this cloud, the higher the chance of rain the higher chance this number will be in the range between 0 and chance
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="e">engine object</param>
        public Cloud(Engine e)
        {
            long ms = Engine.get_current_game_millisecond();

            int world_width  = e.get_current_world().width  * e.get_current_world().tilesize;
            cloud_variant = Engine.generate_int_range(1, UNIQUE_CLOUD_VARIANTS);
            this.position = new Vector2(Engine.generate_int_range(-200,e.get_current_world().width*e.get_current_world().tilesize + 200 ),Engine.generate_int_range(-650,-200)); // between 200 and 350 px above world top border
            creation_time = ms;
            last_update = ms;
            lifetime = Engine.generate_int_range(5000,30000); // lifetime of a cloud between 5 and 30 seconds
            rain = false;
            rain_capacity = 350;                // 350 frames of rain
            scheduled_for_fadeout = creation_time + lifetime;
            opacity_factor = Engine.generate_float_range(0.8f, 1.0f);
            remove = false;
            _emitters = new List<Emitter>();
            rain_start_trigger = Engine.generate_int_range(0, 100);
        }
        //-----------------------------------------
        // interface required functions
        /// <summary>
        /// get a list of emitters
        /// </summary>
        /// <returns>lis of emitters for this object</returns>
        public List<Emitter> get_particle_emitters()
        {
            return _emitters;
        }     
        /// <summary>
        /// add a new emitter to the list
        /// </summary>
        /// <param name="e">emitter object</param>
        public void Add(Emitter e)
        {
            _emitters.Add(e);
        }                   
        /// <summary>
        /// Update emitter position by adding a vector and not replacing it
        /// </summary>
        /// <param name="position_adjustment">position change for x and y coordinates</param>
        public void update_emitter_position(Vector2 position_adjustment)
        {
            foreach(Emitter e in _emitters)
            {
                e.update_position(position_adjustment);
            }
        }
        /// <summary>
        /// Turn particle generation on
        /// </summary>
        public void generate_particle_signal()
        {
            foreach (Emitter e in _emitters)
            {
                e.generate_particle_signal();
            }
        }               
        //-----------------------------------------
        /// <summary>
        /// Get current emitter position
        /// </summary>
        /// <returns>Vector2 with x and y coordinates</returns>
        public Vector2 get_position()
        {
            return position;
        }
        /// <summary>
        /// Get opacity value - the higher it is the more opaque the object is
        /// </summary>
        /// <returns>float value 0 to 1</returns>
        public float get_opacity()
        {         
            return opacity_factor;
        }
        /// <summary>
        /// Get the current rain counter
        /// </summary>
        /// <returns>rain counter value. 0 = empty</returns>
        public int get_rain_capacity()
        {
            return rain_capacity;
        }
        /// <summary>
        /// Update the object
        /// </summary>
        /// <param name="e">engine instance</param>
        public void Update(Engine e)
        {
            long ms = Engine.get_current_game_millisecond();
            // update position with wind speed
            Vector2 adjustment = new Vector2((e.get_wind_speed()/1000.0f)*(ms-last_update), // horizontal affected by wind
                        0);
            position += adjustment;
            update_emitter_position(adjustment); // update all emitters for moving object
            // update rain status and check for fadeout trigger
                if (rain == false && rain_capacity > 0)
                {
                    // update rain status
                    if (rain_start_trigger < e.get_rain_chance()) //chance to become a rain cloud every 300 milliseconds
                    {
                        rain = true;
                        generate_particle_signal(); // signal particle generation
                    }
                }
                else if (rain == true)
                {
                    // adjust longevity of the cloud until rain runs out
                    scheduled_for_fadeout = ms;
                    //  remove some rain capacity
                    rain_capacity--;

                    if (rain_capacity <= 0)
                        rain = false;
                }

            // check if it's time to delete the cloud
                if(ms - scheduled_for_fadeout >= FADEOUT_TIME)
                {
                    if (rain == false || rain_capacity == 0) // only remove normal clouds, rain clouds will disappear after rain is done
                    {
                        e.get_particle_engine().unregister_emitter_enabled_object(this); // unregister particle generation
                        remove = true;                    
                    }
                }
            //update value 
                last_update = ms;
        }
        /// <summary>
        /// get current fade out status for rendering purposes
        /// </summary>
        /// <returns>true or false</returns>
        public bool is_fading_out()
        {
            if (Engine.get_current_game_millisecond() > scheduled_for_fadeout)
                return true;

            return false;
        }
        /// <summary>
        /// get current fade in status for rendering purposes
        /// </summary>
        /// <returns>true or false</returns>
        public bool is_fading_in()
        {
            if ((Engine.get_current_game_millisecond() - creation_time) <= FADEOUT_TIME)
                return true;

            return false;
        }
        /// <summary>
        /// Is the object removable
        /// </summary>
        /// <returns>true or false</returns>
        public bool is_removable()
        {
            return remove;
        }
        /// <summary>
        /// Get the number of milliseconds until fade out
        /// </summary>
        /// <returns>ms value</returns>
        public float get_time_until_fadeout()
        {
            return ((scheduled_for_fadeout + FADEOUT_TIME - Engine.get_current_game_millisecond()) / 1000f);
        }
        /// <summary>
        /// String representation of some stats
        /// </summary>
        /// <param name="e">engine instance</param>
        /// <returns>statistics string</returns>
        public string statistics(Engine e)
        {
            string part1 = "";

            if (!rain)
                part1 = "fading out in: " + ((scheduled_for_fadeout + FADEOUT_TIME - Engine.get_current_game_millisecond()) / 1000f).ToString("00");
            else
                part1 = "[fading out at 0 rain capacity]";

            string part2 = "; particle emitters: " + _emitters.Count;

            return part1 + part2;
        }
        /// <summary>
        /// Get rain cloud status
        /// </summary>
        /// <returns>true or false</returns>
        public bool is_a_rain_cloud()
        {
            return rain;
        }
        /// <summary>
        /// 0f to 1f value for fade-in or fade-out. if fading in - use the value itself, if  fading out - use 1f-value
        /// </summary>
        /// <param name="e">engine</param>
        /// <returns>scale value for the fade effect</returns>
        public double get_fadeout_based_scale_factor(Engine e)
        {
            long ms = Engine.get_current_game_millisecond(); // current ms

            if(is_fading_in()) // if the cloud is fading in - return a fade in factor
            {
                // increasing value
                return e.get_percentage_of_range(creation_time, creation_time + FADEOUT_TIME, ms);
            }

            if(is_fading_out()) // removable cloud fading out
            {
                // increasing value
                return e.get_percentage_of_range(scheduled_for_fadeout, scheduled_for_fadeout + FADEOUT_TIME, ms);
            }

            return 1.0f;
        }
        /// <summary>
        /// Get the texture variant of this cloud for rendering
        /// </summary>
        /// <returns>string value - will be added to texture name in the search function</returns>
        public string get_cloud_variant()
        {
            return cloud_variant.ToString();
        }
    }






/// <summary>
/// Wind simulator
/// Wind speed is number of pixels to move per second. This value should be divided by a thousand and then multiplied by the number of milliseconds since last check 
/// </summary>
    public class Wind
    {
        const float MAX_WIND_SPEED = 10.0f;
        int wind_sustain_duration;  // period of time in ms when the wind speed doesn't change

        float wind_change_value;    // future wind speed value
        float wind_speed;           // current - number of pixels to add to movable particles every frame

        long last_wind_change;      // millisecond when wind last changed direction (speed positive or  negative value)
        int wind_change_duration;   // number of milliseconds over which the speed is moving from previous to new value

        /// <summary>
        /// Constructor
        /// </summary>
        public Wind()
        {
            wind_sustain_duration = Engine.generate_int_range(10000, 20000); // random duration for wind to hold its value
            wind_change_duration = 10000;                                    // change over 10 second period
            last_wind_change = Engine.get_current_game_millisecond();        // time flag for last complete change 
            wind_speed = Engine.generate_float_range(-1.0f, 1.0f);           // initial speed
            wind_change_value = Engine.generate_float_range(-2.0f, 2.0f);    // initial adjustment
        }
        /// <summary>
        /// Update wind values
        /// </summary>
        public void Update()
        {
            float factor = Engine.fade_up(last_wind_change, wind_sustain_duration, wind_change_duration); // find point along the path from previous to new wind speed

            // if path is complete - change overall state and prepare for the next cycle
            if (factor == 1.0f)
            {
                wind_speed += wind_change_value;
                last_wind_change = Engine.get_current_game_millisecond();
                wind_change_value = Engine.generate_float_range(-3.0f, 3.0f);

                // protect against high speeds
                while (wind_speed + wind_change_value > MAX_WIND_SPEED || wind_speed + wind_change_value < -MAX_WIND_SPEED)
                {
                    wind_change_value = Engine.generate_float_range(-5.0f, 5.0f); // recalculate if wind speed goes over boundaries
                }
            }
        }
        /// <summary>
        /// Wind speed
        /// </summary>
        /// <returns>wind speed value</returns>
        public float get_wind_speed()
        {
            float factor = Engine.fade_up(last_wind_change, wind_sustain_duration, wind_change_duration);

            return wind_speed + (wind_change_value * factor);
        }
        /// <summary>
        /// Wind speed delta queued
        /// </summary>
        /// <returns>difference between current and next wind speed</returns>
        public float get_wind_expected()
        {
            return wind_speed + wind_change_value;
        }
        /// <summary>
        /// When will the change be complete
        /// </summary>
        /// <returns>seconds until</returns>
        public string time_until_next_change()
        {
            long ms = Engine.get_current_game_millisecond();

            if (ms - last_wind_change <= wind_sustain_duration)
                return " {hold for:}[188,244,66] " + ((wind_sustain_duration - (ms - last_wind_change)) / 1000).ToString() + ":" + (((wind_sustain_duration - (ms - last_wind_change)) % 1000) / 10).ToString("00");
            else
            {
                long val = (last_wind_change + wind_change_duration + wind_sustain_duration) - ms;
                int sec = (int)(val / 1000);
                int sec2 = (int)(val % 1000) / 10;

                return " {complete in:}[188,244,66] " + sec.ToString("00") + " : " + sec2.ToString("00");
            }
        }
    }
}
