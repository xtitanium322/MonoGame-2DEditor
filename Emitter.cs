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
    /*Particle Emitter class
    An object that takes care of emitting particles into the game World and setting their initial parameters, 
    however, particle life afterwards is taken over by the main Engine class.
    Emitter should be able to spawn particles from a single point (with are defined by circle radius), or a pattern (such as square)
    */
    public class Emitter
    {
        private List<Particle> particles_created;   // a list of all created particles
        private Vector2 position;                   // might change, e.g. bullets of a gun – change position of gun’s muzzle – in this case gun/magic staff object will update the position of where it  needs to shoot projectiles from and emitter’s values will change this way. they can also change based on emitter own trajectory handler 
        private particle_type _particle_type;       // sets up corresponding particle variable 
        private trajectory_type trajectory;         // movement trajectory assigned to particles

        private long creation_time;
        private long last_burst;
        private int particle_lifetime;
        private int emitter_duration;               // emitters can despawn after a period of time – this is a minimum amount of emitter lifetime in ms, if it’s -1 – emitter is considered eternal

        private bool automatic_generation;          // if false an event is needed
        private bool emit;                          // this will be changed on a signal from another class
        private bool random_type;                   // if true – a random particle type will be emitted every time
        private bool random_color_flag;             // create a random tint for particle or use particle’s base tint
        private bool interpolate_color_flag;

        private int rate;                           // number of milliseconds until next particle spawn	
        private int number_of_particles_in_burst;   // how many particles in 1 burst
        private int emitter_radius;                 // area of effect where particle is allowed to spawn, can range from 0px to any number, this will be considered in random number generator when setting Particle’s particle_origin variable. If it’s 0 it will be created at emitter’s origin (defined as a centre point)
        private float acceleration;                 // emitted p[articles will have this acceleration
        private float scale;
        private float rotation_amount;
        private Color particle_base_color;
        private Color? particle_secondary_color;

        /// <summary>
        /// Emitter constructor
        /// </summary>
        /// <param name="pos">cell position vector</param>
        /// <param name="ptype">particle type</param>
        /// <param name="random">randomzied particles</param>
        /// <param name="emitter_life_duration">emitter life</param>
        /// <param name="particle_lifetime">particle lifetime</param>
        /// <param name="rate">number of milliseconds until next burst</param>
        /// <param name="auto">automatic generation - no signal needed</param>
        /// <param name="ttype">particle trajectory type</param>
        /// <param name="number_of_particles_in_burst">number of particles generated in one burst</param>
        /// <param name="random_color">randomized color</param>
        /// <param name="interpolate_color">change from one color to another over time</param>
        /// <param name="area_radius">emitter burst area</param>
        public Emitter(Vector2 pos, particle_type ptype, bool random, int emitter_life_duration,int particle_lifetime, int rate, bool auto, trajectory_type ttype, int number_of_particles_in_burst, bool random_color,bool interpolate_color, int area_radius)
        {
            position = pos;
            this._particle_type = ptype;
            random_type = random;
            emitter_duration = emitter_life_duration;
            last_burst = 0;
            this.rate = rate;
            automatic_generation = auto;
            this.trajectory = ttype;
            this.number_of_particles_in_burst = number_of_particles_in_burst;
            random_color_flag = random_color; // assigns a random color instead of a built in color value
            emitter_radius = area_radius;
            creation_time = Engine.get_current_game_millisecond(); // get ms value from Engine and assign as a beginning point 
            particles_created = new List<Particle>();
            acceleration = 0;
            scale = 1f;
            rotation_amount = 0f;
            particle_base_color = Color.White;
            particle_secondary_color = null;
            emit = false;
            this.particle_lifetime = particle_lifetime;
            interpolate_color_flag = interpolate_color;
        }
//functions
        /// <summary>
        /// Turn particle generation on
        /// </summary>
        public void generate_particle_signal()
        {
            emit = true; // signal the start of particle generation
        }
        /// <summary>
        /// Stop particle generation.If it was set to auto, reactivation will now require a signal regardless
        /// </summary>
        public void remove_particle_signal()
        {
            emit = false;
            automatic_generation = false;
        }
        /// <summary>
        /// update particle type generated by this emitter
        /// </summary>
        /// <param name="p_type">particle type enum value</param>
        public void change_particle_type(particle_type p_type)
        {
            _particle_type = p_type;
        }
        /// <summary>
        /// change the trajectory type of gengerated particles
        /// </summary>
        /// <param name="t_type">trajectory type</param>
        public void change_trajectory_type(trajectory_type t_type)
        {
            trajectory = t_type;
            
        }
        /// <summary>
        /// Update particle color - will affect every future generated particle
        /// </summary>
        /// <param name="clr">particle color</param>
        public void change_particle_color(Color clr)
        {
            particle_base_color = clr;
            random_color_flag = false;
            interpolate_color_flag = false;
        }
        /// <summary>
        /// Acceleration value for the future particles
        /// </summary>
        /// <param name="val">float value any range</param>
        public void set_emitter_acceleration(float val)
        {
            acceleration = val;
        }
        /// <summary>
        /// Get a list of all particles created by all emitters
        /// </summary>
        /// <returns>A list of Particle type</returns>
        public List<Particle> get_particles()
        {
            return particles_created;
        }
        /// <summary>
        /// Get emitter position
        /// </summary>
        /// <returns>cell address/position of the emitter</returns>
        public Vector2 get_position()
        {
            return position;
        }
        /// <summary>
        /// Set new emitter position
        /// </summary>
        /// <param name="val">cell address (not the world_map address)</param>
        public void set_position(Vector2 val)
        {
            position = val;
        }
        /// <summary>
        /// Update position of the emitter by adding to current cell address, not replacing it
        /// </summary>
        /// <param name="adjustment">adjustemnt to current position</param>
        public void update_position(Vector2 adjustment)
        {
            position += adjustment;
        }
        /// <summary>
        ///  Update particle list after Update function removes some due to lifetime calculations
        /// </summary>
        /// <param name="updated">A list of particles after update</param>
        public void update_particle_list(List<Particle> updated)
        {
            particles_created = null;
            particles_created = updated;
        }
        /// <summary>
        /// Enable color interpolation between two colors
        /// </summary>
        /// <param name="clr">Secondary particle color</param>
        /// <param name="val">true - to enable, false - to use default</param>
        public void enable_color_interpolation(Color clr, bool val)
        {
            interpolate_color_flag = val;

            if (val)
                particle_secondary_color = clr;
            else
                particle_secondary_color = null;
        }
        /// <summary>
        /// Update particle lifetime
        /// </summary>
        /// <param name="val">new lifetime value in milliseconds</param>
        public void change_particle_lifetime(int val)
        {
            particle_lifetime = val;
        }
        /// <summary>
        /// Update Emitter generation radius - number of pixels in every direction where particle can be generated. Randomly decided.
        /// </summary>
        /// <param name="val">Radius value, to use point generation - 1.Values must be between 1 and 300, automatically fixed if outside the bounds </param>
        public void change_emitter_radius(int val)
        {
            // safety
            if (val > 300)
                val = 300;
            if (val < 1)
                val = 1;

            emitter_radius = val;
        }
        /// <summary>
        /// Update number of particles generated in a single burst cycle
        /// </summary>
        /// <param name="val">Burst amount</param>
        public void change_emitter_burst_amount(int val)
        {
            number_of_particles_in_burst = val;
        }
        /// <summary>
        /// Update angular momentum - number of degrees of rotation in one cycle
        /// </summary>
        /// <param name="val">Float value - radians. PI = 3.14 radians = 180 degrees</param>
        public void update_rotation_amount(float val)
        {
            rotation_amount = val;
        }
        /// <summary>
        /// Update particle scale
        /// </summary>
        /// <param name="val">Any float value. 1f represents a standard/original scale</param>
        public void update_scale(float val)
        {
            scale = val;
        }
        /// <summary>
        /// Update how many milliseconds should pass between bursts
        /// </summary>
        /// <param name="val">Number of milliseconds</param>
        public void update_creation_rate(int val)
        {
            rate = val;
        }

        /// <summary>
        /// generates particle when called, can also be automated with a 'rate' ms between bursts. Particle object can the be added to particles list in ParticleEngine
        /// used in update() in this class
        /// emitter flags will update particles
        /// </summary>
        /// <param name="origin">Particle position in pixels, in relation to map origin</param>
        /// <param name="lifetime">Particle lifetime value (ms)</param>
        /// <param name="acceleration">Particle acceleration/per second. This many pixels per second will be added to speed every second.</param>
        /// <param name="direction_angle">Direction of the movement - currently unused</param>
        /// <param name="angular_momentum">Rotational speed of the particle</param>
        /// <returns>Particle object</returns>
        public Particle generate_particle(Vector2 origin, int lifetime, float acceleration, float direction_angle, float angular_momentum)
        {
            Particle temp = new Particle(
                _particle_type,
                trajectory,
                origin,
                lifetime, // lifetime of particle
                Int32.MaxValue, //distance traveled before expiration
                angular_momentum,
                0f,
                acceleration,
                direction_angle// straight downward = MAth.Pi
            );

            // --------------------------------------------------------create values based on emitter properties
            // color
            Color tint = Color.White;
            Color interpolate_to = Color.White;

            if (random_color_flag)
            {
                tint = new Color(
                    Engine.generate_int_range(0, 255),
                    Engine.generate_int_range(0, 255),
                    Engine.generate_int_range(0, 255));

                temp.set_color(tint);
            }
            else
            {
                if (particle_base_color != null)
                    tint = particle_base_color;

                temp.set_color(tint);
            }
            // randomized color interpolation
            if (this.interpolate_color_flag)
            {
                // if there is no secondary color
                if (particle_secondary_color == null)
                {
                    interpolate_to = new Color(
                        Engine.generate_int_range(0, 0),
                        Engine.generate_int_range(0, 0),
                        Engine.generate_int_range(0, 0));
                }
                else
                {
                    interpolate_to = (Color)particle_secondary_color;
                }
                // if there is
                temp.enable_color_features(tint, interpolate_to, Engine.generate_int_range(1000, 2000)); // sec interpolation
            }
            else
            {
                temp.disable_color_features(false, false);
            }
            // update particle scale
            temp.set_current_scale(scale);

            return temp;
        } 
        /// <summary>
        /// used to check if it’s time to remove this emitter from the list of active emitters in ParticleEngine
        /// </summary>
        /// <param name="current_time">Millisecond value - when the particle is scheduled for deletion</param>
        /// <returns>True - for deletion and false - for continued existence</returns>
        public bool terminated(int current_time)
        {
            if (current_time > creation_time + emitter_duration)
                return true;

            return false;
        }
        /// <summary>
        /// Get automated generation status
        /// </summary>
        /// <returns>true if particles are auto-generated, false if a signal is required. Send signal by using generate_particle_signal()</returns>
        public bool has_automatic_generation()
        {
            return automatic_generation;
        }

        /// <summary>
        /// Update function 
        /// </summary>
        /// <param name="engine">Engine instance</param>
        public void update(Engine engine)
        {
            long ms = Engine.get_current_game_millisecond();

            // generate particles
            if ((automatic_generation || emit)
                && Engine.get_current_game_millisecond() - last_burst >= rate)
            {
                // add particle to a general collection
                switch (_particle_type)
                {
                    case particle_type.raindrop:
                        for (int i = 0; i < number_of_particles_in_burst; i++) // burst
                        {
                            engine.get_particle_engine().add_particle(generate_particle(
                                position
                                + new Vector2(Engine.generate_int_range(-emitter_radius, emitter_radius), Engine.generate_int_range(0,i*3)), // origin with randomized position of creation
                                particle_lifetime, // lifetime between 1 and 3 seconds
                                Engine.gravity_value, // acceleration / per second based on gravity
                                (float)Math.PI,       // direction
                                0f                    // angular momentum (rotation value)
                                ));
                        }
                        last_burst = ms;
                        //adjust rate
                        rate = Engine.generate_int_range(300, 600);
                        break;
                    case particle_type.star:
                        for (int i = 0; i < number_of_particles_in_burst; i++) // burst
                        {
                            engine.get_particle_engine().add_particle(generate_particle(
                                position + new Vector2(Engine.generate_int_range(-emitter_radius, emitter_radius), Engine.generate_int_range(-emitter_radius, emitter_radius)), // position of origin
                                particle_lifetime, // lifetime 
                                acceleration, // acceleration / per second based on gravity
                                (float)Math.PI, // direction
                                rotation_amount// rotation
                                ));
                        }
                        last_burst = ms;
                        //adjust rate
                        rate = Engine.generate_int_range(1, 2);
                        break;
                    //all others
                    case particle_type.circle: case particle_type.hollow_square: case particle_type.square: case particle_type.triangle: case particle_type.x:
                        for (int i = 0; i < number_of_particles_in_burst; i++) // burst
                        {
                            engine.get_particle_engine().add_particle(generate_particle(
                                position + new Vector2(Engine.generate_int_range(-emitter_radius, emitter_radius), Engine.generate_int_range(-emitter_radius, emitter_radius)), // position of origin
                                particle_lifetime, // lifetime 
                                acceleration,    // acceleration / per second based on gravity
                                (float)Math.PI,  // direction
                                rotation_amount //  rotation
                                ));
                        }
                        last_burst = ms;
                        //adjust rate
                        rate = Engine.generate_int_range(1, 2);
                        break;
                    default:
                        break;
                }

            }
        }


    }// class end
}// namespace end
