using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace beta_windows
{
    /*Animated Texture and Particle concept 
    Completed design: supports various animation types handled by Animation Engine class. 
    Need to add XNA rendering algorithms.
    Goal = handle at least 10k particles on screen without throttling framerate
    Add an option for a particle to spawn another particle on it's own position. 
    Particle maximum travel distance value.
    */
    public enum animation_type { circular, oscillation, reverse_circular }; // types of animation mean the order of frame switching
    public enum particle_type { unknown };

    public struct Sprite
    {
        private string name; // unique identifier to do the search on.Example: mainchar_running_left, mainchar_running_right, mainchar_taking_damage, mainchar_levelup etc.
        private Texture2D source;
        private int w; // width in px (width of one frame, not the whole spritesheet)
        private int h; // height in px 
        private int frames; // number of frames in entire animation cycle
        private int current_frame; // which frame of the cycle is currently being displayed
        private int cycle_duration; // ms for entire collection to loop – will speed up or slow down the animation
        private int frame_duration; // a calculated value – depending on total frames how long should it take to switch to next frame given a cycle_duration
        private float scale; // default = 1 – size of the rendered image in proportion to stored image, can be changed if necessary
        private long tick_counter; // time value of last frame switch
        private animation_type animation; // circular, oscillation, reverse circular – defines how texture current active frames are changed
        private bool _static; // true = no animation switch.frames value should be 1
        private bool forward_switch; // true = forward, false = reverse
        private bool paused;
        private particle_type _particle_type; // a texture can be used as a particle archetype. In ParticleEngine type of particle variable will fetch an AnimatedTexture stored in AnimationEngine to draw.
        // constructor section
        public Sprite(string name, Texture2D source, int width, int height, int frames, int duration, animation_type animation, bool forward_switch, bool _static, particle_type ptype)
        {
            // struct initializations here
            this.name = name;
            w = width;
            h = height;
            this.frames = frames;
            cycle_duration = duration;
            // calculate a frame_duration variable here
            frame_duration = cycle_duration / frames; // calculates as int, drop any fractions of millisecond
            this.animation = animation;
            this.forward_switch = forward_switch; // give initial value to switch direction, used by oscillating switches
            this._static = _static;
            this._particle_type = ptype;
            // other
            tick_counter = 0;
            scale = 1.0f;
            current_frame = 1;
            paused = false;

            this.source = source;
        }
        // sprite loading section
        public void load_sprite(Texture2D source)
        {
            this.source = source; // can’t be done in constructor, should be loaded separately
        }
        // center calculation 
        public Int2 center_of_frame()
        {
            return new Int2(w / 2, h / 2);
        }

        public Vector2 center_of_frame_vector()
        {
            return new Vector2(w / 2, h / 2);
        }
        // Rectangle calculation
        public Rectangle current_rectangle() // this function handles cropping the image based on what frame is animated. used in XNA draw function
        {// 0 height for 1 row sprite sheet
            return new Rectangle(w * (current_frame - 1), 0, w, h);
        }
        // Update function
        public void update(long current_value) // update frame based on current tick value
        {
            //skip if paused
            if (paused)
                return;
            // if not paused - continue with updates
            if (current_value >= tick_counter + frame_duration) // enough time since last switch to allow another switch
            {
                //switching algorithm according to animation enumeration
                switch (animation)
                {
                    case animation_type.circular: // standard 1-2-3-1-2-3 etc. increase frame number and switch back to 1 at overflow point
                        if (current_frame != frames)
                            current_frame++;
                        else
                            current_frame = 1;
                        break;
                    case animation_type.oscillation: // standard 1-2-3-2-1-2-3 etc. as number reaches either 1 or max frame switch direction and increase/decrease 
                        if (forward_switch)
                        {
                            if (current_frame != frames)
                                current_frame++;
                            else
                            {
                                current_frame--;
                                forward_switch = false;
                            }
                        }
                        else if (!forward_switch)
                        {
                            if (current_frame != 1)
                            {
                                current_frame--;
                            }
                            else
                            {
                                current_frame++;
                                forward_switch = true;
                            }
                        }
                        break;
                    case animation_type.reverse_circular: // standard 3-2-1-3-2-1 etc. decrease frame number and switch back to max frame at overflow point 
                        if (current_frame != 1)
                            current_frame--;
                        else
                            current_frame = frames;

                        break;
                    default:
                        break;
                }
            }
        }
        // get current frame outside of this class
        public int get_current_frame()
        {
            return current_frame;
        }
        // pause the animation cycle
        public void set_pause(bool value)
        {
            paused = value;
        }
        public void draw_from_center(Vector2 position) // since 1 texture can be used by many rendered objects, position has to be supplied by, e.g. Particle itself
        {//xna integrated rendering algorithm adjusted for center of image
        }
        public void draw_from_origin(Vector2 position)
        {//xna integrated rendering algorithm adjusted for image origin
        }

        public string get_name()
        {
            return name;
        }

        public particle_type get_particle_type()
        {
            return _particle_type;
        }
    }

    /*Animated Texture updater and container*/
    public class AnimationEngine
    {
        private List<Sprite> sprites; // each particle archetype will be using ONE animatedtexture object, however it can be scaled, rotated and color differently based on the particle itself. As a result all particles will have a synced animation cycle.
        // functions
        public void add_texture(Sprite a)
        {
            sprites.Add(a);
        }

        public void update_animations(int current)
        {
            foreach (Sprite a in sprites)
            {
                a.update(current);
            }
        }
        // a returned animated texture is then drawn using it’s active rectangle adjusted drawing function
        public Sprite? find_sprite(string name)
        {
            foreach (Sprite a in sprites)
            {
                if (a.get_name() == name)
                    return a;
            }
            return null; // if null is returned – handle in a calling function, either ignore or raise an exception
        }
        // a returned animated texture is then drawn using it’s active rectangle adjusted drawing function
        public Sprite? find_sprite(particle_type _particle_type)
        {
            foreach (Sprite a in sprites)
            {
                if (_particle_type != null && a.get_particle_type() == _particle_type)
                    return a;
            }

            return null; // if null is returned (there is no correct particle type) – handle in a calling function, either ignore or raise an exception
        }
    }
}
