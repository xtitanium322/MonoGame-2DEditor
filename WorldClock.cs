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
{
    /// <summary>
    /// 24 hour in-game clock class - base of the day/night cycle and related gameplay functions
    /// </summary>
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
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="rate">number of milliseconds between updates</param>
        /// <param name="rm">number of clock seconds added per cycle</param>
        public WorldClock(int rate, int rm)
        {
            hours = 12;                  // hours must not go above 23
            minutes = 0;
            seconds = 0;
            millisecond_marker = 0;
            rate_multiplier = rm;
            this.rate = rate;
            paused = false;
        }
        /// <summary>
        /// Set clock values
        /// </summary>
        /// <param name="h">hours</param>
        /// <param name="m">minutes</param>
        public void set_clock(int h, int m)
        {
            hours = h;
            minutes = m;
            seconds = 0;
        }
        /// <summary>
        /// Set clock using all values
        /// </summary>
        /// <param name="h">hours</param>
        /// <param name="m">minutes</param>
        /// <param name="s">seconds</param>
        public void set_clock(int h, int m, int s)
        {
            hours = h;
            minutes = m;
            seconds = s;
        }
        /// <summary>
        /// Check if the clock is paused
        /// </summary>
        /// <returns>true or false</returns>
        public bool get_paused_status()
        {
            return paused;
        }
        /// <summary>
        /// Set pause status
        /// </summary>
        /// <param name="value">true or false</param>
        public void set_paused(bool value)
        {
            paused = value;
        }
        /// <summary>
        /// get raw time 
        /// </summary>
        /// <returns>a vector containing hours, minutes and seconds</returns>
        public Vector3 get_raw_time_vector()
        {
            return new Vector3(hours, minutes, seconds);
        }
        /// <summary>
        /// get raw time in minutes
        /// </summary>
        /// <returns>converted hours + minutes into minutes</returns>
        public int get_time_in_minutes()
        {
            return hours * 60 + minutes;
        }
        /// <summary>
        /// Convert time to seconds
        /// </summary>
        /// <returns>converted hours + minutes into seconds</returns>
        public int get_time_in_seconds()
        {
            return hours * 3600 + minutes * 60 + seconds;
        }
 
        /// <summary>
        /// get current clock time
        /// </summary>
        /// <returns>a vector containing hours, minutes and seconds converted into 12 hour mode</returns>
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
        /// <summary>
        /// AM or PM tag for a 12 hour clock representation
        /// </summary>
        /// <returns>string am or pm value</returns>
        public String get_am_pm()
        {
            if (hours < 12)
            {
                return "AM";
            }
            else
                return "PM";
        }
        /// <summary>
        /// add minute to the clock
        /// </summary>
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
        // add second to the clock
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
                seconds++; // rate of added seconds per cycle
        }
        /// <summary>
        /// Update the game clock
        /// </summary>
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
        // 
        /// <summary>
        /// Update function for game clock - run in Game class Update function
        /// </summary>
        /// <param name="milliseconds">current millisecond</param>
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
