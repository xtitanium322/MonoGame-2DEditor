using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace EditorEngine
{   // 24 hour in-game clock class - base of the day/night cycle and related gameplay functions
    public class WorldClock
    {
        private int hours;
        private int minutes;
        private int seconds;
        private int rate;                // 1 game world second to real life milliseconds ratio
        private int rate_multiplier;
        private long millisecond_marker;  // number of milliseconds passed in game time since last gameplay-minute change
        private bool paused;

        public WorldClock()
        {

        }
        public WorldClock(int rate, int rm)
        {
            hours = 19; // hours must not go above 23
            minutes = 0;
            seconds = 0;
            millisecond_marker = 0;
            rate_multiplier = rm;
            this.rate = rate;
            paused = false;
        }
        // functions
        // adjust game clock
        public void set_clock(int h, int m)
        {
            hours = h;
            minutes = m;
            seconds = 0;
        }
        public void set_clock(int h, int m, int s)
        {
            hours = h;
            minutes = m;
            seconds = s;
        }
        public bool get_paused_status()
        {
            return paused;
        }
        public void set_paused(bool value)
        {
            paused = value;
        }
        // get raw time 
        public Vector3 get_raw_time_vector()
        {
            return new Vector3(hours, minutes, seconds);
        }
        // get raw time in number of minutes or seconds
        public int get_time_in_minutes()
        {
            return hours * 60 + minutes;
        }
        public int get_time_in_seconds()
        {
            return hours * 3600 + minutes * 60 + seconds;
        }
        // get current clock time
        public Vector3 get_time()
        {
            int hour_value;
            // translate for 12 hour clock
            if (hours == 12)
            {
                hour_value = 12;
            }
            else if (hours < 12)
            {
                hour_value = hours;
            }
            else
            {
                hour_value = hours % 12;
            }

            return new Vector3(hour_value, minutes, seconds); // x = hours, y = minutes, z = seconds (display clock in AM/PM format)
        }
        // for a 12 hour clock representation
        public String get_am_pm()
        {
            if (hours < 12)
            {
                return "AM";
            }
            else
                return "PM";
        }
        // add minute to game time
        public void add_minute()
        {
            if (minutes == 59)
            {
                minutes = 0;

                if (hours >= 23)
                    hours = 0;
                else
                    hours++;
            }
            else
                minutes++;
        }
        public void add_second()
        {
            // seconds level
            if (seconds >= 59)
            {
                seconds = 0;
                // minute level
                if (minutes >= 59)
                {
                    minutes = 0;

                    // hour level
                    if (hours >= 23)
                        hours = 0;
                    else
                        hours++;
                }
                else
                    minutes++;
            }
            else
                seconds++/* rate_multiplier*/; // rate of added seconds per cycle
        }

        public void update_clock()
        {
            if (paused)
                return; // do not update clock if it'engine paused

            // based on the rate_multiplier (number of seconds to add in 1 update) adjust current clock
            // calculate how many minutes and seconds need to be added, then add seconds first and add minutes after that
            int minutes_updated = rate_multiplier / 60;
            int seconds_updated = rate_multiplier % 60;

            // handle seconds
            if (seconds + seconds_updated >= 60)
            {
                add_minute();
                seconds = (seconds + seconds_updated) % 60;
            }
            else
            {
                seconds += seconds_updated;
            }
            // handle minutes
            if (minutes + minutes_updated >= 60)
            {
                if (hours >= 23)
                {
                    hours = 0;
                    minutes = (minutes + minutes_updated) % 60;
                }
                else
                {
                    hours++;
                    minutes = (minutes + minutes_updated) % 60;
                }
            }
            else
            {
                minutes += minutes_updated;
            }
        }
        // Update function for game clock - run in Game class Update function
        public void update(long milliseconds)
        {
            if (milliseconds >= millisecond_marker + rate)
            {
                // true = game minute has passed
                //add_second();
                update_clock();
                millisecond_marker = milliseconds;
            }
        }
    }
}
