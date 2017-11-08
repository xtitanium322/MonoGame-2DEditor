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
    /// Particle trajectories of movement
    /// </summary>
    public enum trajectory_type { laser_line, chaotic, fall, rise, weightlessness, ballistic_curve, static_, upward_fountain, homing_missile };
    
    /// <summary>
    /// Interface - ensures proper particle creation for any unrelated classes
    /// </summary>
    public interface IParticleCreating
    {
        /// <summary>
        /// each such entity must have a list of emitters
        /// </summary>
        List<Emitter> emitters
        {
            get;
            set;
        }

        //1 returns a list of emitters which contain a list of particles (preferrably from the emitter inside emitters list
        //2 should add to emitters     
        //3 since most objects emitting particles tend to move - update is required
        //4 this signals the particle creation for non-automatic emitters

        List<Emitter> get_particle_emitters();
        void Add(Emitter e);
        void update_emitter_position(Vector2 position_adjustment);
        void generate_particle_signal();
    }
    /// <summary>
    /// ParticleEngine class
    /// This class manages entire particle system. Most overarching functions must go here.
    /// After Particle has been created by Emitter it will be modified by Particle Engine and it’s movement, rotations and other transformations will be handled here.
    /// Stress test - 14k particles  - 60fps stable on rx380x graphics card 
    /// </summary>
    public class ParticleEngine
    {
        private bool additive_blending_flag; // this defines if all particles (draw to their own 
        private List<IParticleCreating> emitting_objects; // source of particles: assigns a starting position, direction, movement trajectory, type of particle generated, scaling behavior, lifetime etc. These object only generate particles, everything else is handled by particle engine update cycle which loops through all particles in the list and based on their trajectory, scale behaviour, direction, rotation each one will change position and orientation.
        private List<Particle> particles;
        //List<Particle> processing;                    // temporary list for drawing sequence
        /// <summary>
        /// Constructor
        /// </summary>
        public ParticleEngine()
        {
            emitting_objects = new List<IParticleCreating>();
            particles = new List<Particle>(30000);               // create with enough space for 10k particles
            //processing = new List<Particle>(30000); 
            additive_blending_flag = false;
        }

        /// <summary>
        /// Adds a new particle to the main list
        /// </summary>
        /// <param name="p">Particle object to be added</param>
        public void add_particle(Particle p)
        {
            particles.Add(p);
        }
        /// <summary>
        /// Get all the particles generated so far (only the ones that are still active)
        /// </summary>
        /// <returns>A List of particle objects</returns>
        public List<Particle> get_particles()
        {
            return particles;
        }
        /// <summary>
        /// Unregister a particle enabled object 
        /// </summary>
        /// <param name="instance">object to unregister - in case of deletion or expiration</param>
        public void delete_emitter(IParticleCreating instance) // and all its particles, or just the emitter but keep particles active
        {
            // remove element from the list when Emitter is no longer needed 
            emitting_objects.Remove(instance);
        }
        /// <summary>
        /// Get a total number of particles in the list
        /// </summary>
        /// <returns>Particle count</returns>
        public int count_particles()
        {
            int count = 0;

            foreach (Particle p in particles)
            {
                count++;
            }

            return count;
        }
        /// <summary>
        /// All registered objects can have their particle emitters and by extension particle lists updated
        /// </summary>
        /// <param name="e">Engine object</param>
        public void update(Engine engine)// Update all dynamic parameters
        {
            //update all emitters
            foreach (IParticleCreating ipc in emitting_objects)
            {
               foreach(Emitter e in ipc.get_particle_emitters())
               {
                   e.update(engine);
               }
            }
            // defines the algorithm handling movement, rotation, shape/scale change and lifetime of particle as well as collision detection and signalling events (event such as particle hit a block, a player, an enemy, another particle)
            foreach (Particle p in particles)
            {    
                // check for collision with the game world. 1 calculate cell address based on position 2. check if cell is not air
                Vector2 current_cell = engine.get_current_world().vector_position_to_cell(p.get_position());
                if (engine.get_current_world().get_tile_id(current_cell) != 0 
                    && engine.get_current_world().get_tile_id(current_cell) != 999) // outside of the building block range
                {
                    p.set_delete_signal();
                }
                // update particle scale
                if (p.has_dynamic_scale())
                {
                    // use an interpolation function from main game engine class to select a value from min/max pair based on current time value
                    // a standard sine oscillation function integrated with current game time value
                    p.set_current_scale(Engine.fade_sine_wave_smooth(p.get_scale_fluc_period(), p.get_scale_minmax().X, p.get_scale_minmax().Y)); // automatically uses current game millisecond
                }
                // update particle transparency
                if (p.has_dynamic_transparency())
                {
                    // use an interpolation function from main game engine class to select a value from min/max pair based on current time value
                }
                // update particle speed + position
                //--------- update particle position
                switch (p.get_trajectory_type()) // based on pre-defined value calculate new coordinates for this particle
                {// for collision purposes need to know if line between two points (old and new) intersect collision rectangle of any object. Use the geeksforgeeks c++ program example to check intersection between a ballistic line and each of the 4 lines of collision bounding rectangle
                    // update position
                    // check for collision with visible* ground/props and create a collision event?
                    // check for collision with active characters and create a collision event?
                    //* any objects that are on screen. For characters this check is unnecessary
                    // all trajectories so far:straight line laser, chaotic, fall, rise, weightlessness, ballistic trajectory, static, upward fountain, homing missile
                    case trajectory_type.laser_line: // calculate by taking current point, direction angle, 0 value for gravity, speed and acceleration and use in a physics formula
                        // currently - simply move in a straight line
                        p.adjust_position(new Vector2(
                            Engine.convert_rate(3400f, Engine.get_current_game_millisecond() - p.get_last_update()), // speed per second
                            0f)); // gravity = 0
                        break;
                    case trajectory_type.chaotic: // take current point, particle speed and acceleration and transfer it in a random direction
                        //p.enable_dynamic_scale(0.9f, 1.00f, 1000); // testing scale dynamic                     
                        p.adjust_position(
                            new Vector2( 
                                Engine.generate_float_range(-3f, 3f), 
                                Engine.generate_float_range(-3f, 3f)
                                )); // chaos factor of 3 pixels per frame
                        break;
                    case trajectory_type.fall: // apply local gravity value and push particle down 
                        p.adjust_position(new Vector2( 0, p.get_speed())); // any speed that this particle has - apply downward
                        break;
                    case trajectory_type.rise:// apply 0 gravity and push particle up using speed and acceleration value
                        //p.enable_dynamic_scale(0.7f, 1.00f, 1000); // testing scale dynamic   
                        p.adjust_position(new Vector2( 0, -p.get_speed())); // any speed that this particle has - apply upward
                        break;
                    case trajectory_type.weightlessness: // apply sinewave function to float the particle around it’s original point in space for 2 axis x and y. slow movement
                        break;
                    case trajectory_type.ballistic_curve: // apply local gravity value and all other parameters to send particle in a curve
                        p.adjust_position(new Vector2(
                            Engine.convert_rate(1000f,Engine.get_current_game_millisecond() - p.get_last_update()), // 1000 pixels per second horizontal speed value (can be set up to be particle inherent value)
                            p.get_speed())); // gravity
                        break;
                    case trajectory_type.static_:
                        // no position changes particle stays frozen in space
                        break;
                    case trajectory_type.upward_fountain: // similar to straight up rise but with a random narrow angle direction for each particle. this will cause many particles to form a cone of random angle
                        break;
                    case trajectory_type.homing_missile: // based on a target send this particle towards it
                        break;
                    default: // no specific value
                        break;
                }
                //--------- Update wind value
                if(p.get_particle_type() == particle_type.raindrop) // only for raindrops
                    p.adjust_position(Engine.convert_rate(engine.get_wind_speed(), Engine.get_current_game_millisecond() - p.get_last_update()), true);

                // update particle rotation
                p.rotate_particle();     // adds particle's angular momentum to existing particle rotation angle based on timing
                // set last update time
                p.refresh_last_update(); 
            }

            for(int j = particles.Count - 1; j >=0; j--)
            {
                // check existence limits
                if (particles[j].get_delete_signal() || particles[j].get_creation_time() + particles[j].get_particle_life() <= Engine.get_current_game_millisecond())
                {
                    // particle is deleted
                    particles.RemoveAt(j);
                }
            }
        }
        /// <summary>
        /// Called in main Game Draw() - will create all of the particles on a separate rendering layer
        /// </summary>
        /// <param name="gameTime">Game1 built-in object that controls rendering</param>
        public void Draw(GameTime gameTime, Engine engine)
        {
            // prepare textures
            // need to draw every instance of each type ion the same sequence, to eliminate switching overhead)
            var particle_types = Enum.GetValues(typeof(particle_type));

            foreach (particle_type prt in particle_types)
            {
                // skip unknown particles
                if (prt == particle_type.unknown)
                    continue;

                // draw this sprite
                Texture2D current_sprite = engine.get_texture(prt.ToString());

                // draw sequence : updated
                for (int i = particles.Count-1; i >= 0; i--)
                {
                    if (particles[i].get_particle_type() != prt)
                    {
                        continue;
                    }

                    if (engine.is_within_visible_screen(particles[i].get_position() - engine.get_camera_offset()))
                    {
                        engine.xna_draw(current_sprite, particles[i].get_position() - engine.get_camera_offset(), null, particles[i].get_color(), particles[i].get_particle_angle(), new Vector2(current_sprite.Width / 2, current_sprite.Height / 2), particles[i].get_current_scale(), SpriteEffects.None, 1f);
                    }
                }
            }
        }
        /// <summary>
        /// this is a concept supporting function that should be called by update any time there is a collision of particle with any World active object such as a ground block. 
        /// Based on what particle collided with call that object’s function, e.g. receive_bullet_damage() or receive_magic_damage() 
        /// etc and then delete the particle or let it fly through and live out the rest of particle_life ticks before being cleaned up by managing class.
        /// </summary>
        public void create_collision_event() 
        {
            // check for collision between this particles bounding box and any object of interest (that exist in near area)
            // invoke a function on an object collided with and then remove the particle or let it continue 
        }
        /// <summary>
        /// Register the object so it's emitters can be updated
        /// </summary>
        /// <param name="instance">An emitter hosting object</param>
        public void register_emitter_enabled_object(IParticleCreating instance)
        {
            emitting_objects.Add(instance);
        }
        /// <summary>
        /// Remove a particle enabled object from the list of registered emitting objects
        /// </summary>
        /// <param name="instance">instance of the particle interface implementing class</param>
        public void unregister_emitter_enabled_object(IParticleCreating instance)
        {
            emitting_objects.Remove(instance);
        }
        /// <summary>
        /// Emitter generator - send emitter for an enabled object to handle
        /// </summary>
        /// <param name="host">an object that is hosting the emitter</param>
        /// <param name="pos">emitter position in pixels</param>
        /// <param name="ptype">particle type being generated</param>
        /// <param name="random">are particles randomized?</param>
        /// <param name="emitter_life_duration">how long should this emitter exist</param>
        /// <param name="rate">number of ms between bursts</param>
        /// <param name="auto">is generation automatic?</param>
        /// <param name="ttype">particle trajectory type</param>
        /// <param name="particle_lifetime">how long should the particles exist - in ms?</param>
        /// <param name="number_of_particles_in_burst">number of particles generated at the same time</param>
        /// <param name="random_color">are colors randomized?</param>
        /// <param name="interpolate_color_flag">are colors interpolated?</param>
        /// <param name="area_radius">area of generation</param>
        public void create_emitter(
            IParticleCreating host,             // emitter will be created inside this object
            Vector2[] pos,                      // create in these positions (allows creation of multiple emitters in one function call)
            particle_type ptype,                // defines what shape the particle takes as well as multiple internal properties
            bool random,                        //       
            int emitter_life_duration,          // when is this emitter scheduled for deletion
            int rate,                           // number of milliseconds between particle bursts
            bool auto,                          // requires no manual input to create particles
            trajectory_type ttype,              // trajectory type - defines particle movement after creation  
            int particle_lifetime,              // particle lifetime
            int number_of_particles_in_burst,   // number of particles created in the same cycle
            bool random_color,                  // randomizes particle tint
            bool interpolate_color_flag,        // change from current color to black
            int area_radius                     // all particles in one burst will be created in this radius (use 1 for point creation)            
            )
        {
            for (int i = 0; i < pos.Length; i++)
            {
                Emitter temp = new Emitter(pos[i], ptype, random, emitter_life_duration, particle_lifetime, rate, auto, ttype, number_of_particles_in_burst, random_color,interpolate_color_flag, area_radius); // call Emitter constructor using parameters supplied by Engine
                host.Add(temp); // function required by the interface
            }
        }
    }
}
