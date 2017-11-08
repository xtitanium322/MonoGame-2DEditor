using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Xml;                                                           // use xml files
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;  // xml serialization
using MyDataTypes;                                                          // data types structure
using System.IO;

namespace EditorEngine
{
    /// <summary>
    /// World + filename for the tile xml file
    /// </summary>
    public struct WorldStruct   // Structure representing worlds in the World list
    {
        public string filename;
        public World world;

        public WorldStruct(String filename, World w)
        {
            this.filename = filename;
            world = w;
        }
    }
    /// <summary>
    /// very world in the game + functions that let player interact with these worlds
    /// </summary>
    public class WorldCollection
    {
        public List<WorldStruct> worlds;    // list of all playable worlds
        int current;                        // index of the current world
        // constructor
        public WorldCollection()
        {
            worlds = new List<WorldStruct>();    // create a list for game worlds
            current = 0;
        }
        /// <summary>
        /// Get a list of worlds
        /// </summary>
        /// <returns>List of WorldStruct objects</returns>
        public List<WorldStruct> get_worlds()
        {
            return worlds;
        }
        /// <summary>
        /// add world to world list
        /// </summary>
        /// <param name="filename">filename for serialization</param>
        /// <param name="w">World object</param>
        public void add_world(String filename, World w)
        {
            if (worlds.Count == 0)
                current = 0;

            worlds.Add(new WorldStruct(filename, w));
        }
        /// <summary>
        /// Load the world into xml file
        /// </summary>
        /// <param name="engine">Engine instance</param>
        public void load_tiles(Engine engine)
        {
            foreach (WorldStruct w in worlds)
            {
                ArrayList tile_map = new ArrayList();

                try
                {

                    using (FileStream stream = new FileStream(w.filename, FileMode.Open)) //open xml file, close??
                    {
                        if(stream != null)
                        using (XmlReader reader = XmlReader.Create(stream)) // open file in xml reader
                        {

                            tile_map = IntermediateSerializer.Deserialize<ArrayList>(reader, null);
                            //tile_map = tiles.Cast<MapData>().ToList(); // load into the list
                        }
                    }
                    // generating map tiles
                    int total_count = tile_map.Count;

                    for (int i = 0; i < total_count; i++ )
                    {
                        MapData map_object = (MapData)tile_map[i];
                        if (tile_map[i] != null)
                        {
                            for (int x = 0; x < map_object.width; x++)
                            {
                                for (int y = 0; y < map_object.height; y++)
                                {
                                    if (map_object.block_name >= 0)
                                        w.world.generate_tile((short)map_object.block_name,
                                        map_object.block_x + x,
                                        map_object.block_y + y,
                                        engine);
                                    else
                                        w.world.generate_tile((short)map_object.block_name,
                                                 map_object.block_x + x,
                                                 map_object.block_y + y,
                                                 engine,
                                                 map_object.water_content);
                                }
                            }
                        }
                    }
                }
                catch (FileNotFoundException e)
                {
                    Debug.WriteLine("[DEBUG INFO] File doesn't exist: " + e);
                }
                catch(NullReferenceException e)
                {
                    Debug.WriteLine("[DEBUG INFO] File doesn't exist: " + e);
                }
                catch(InvalidCastException e)
                {
                    Debug.WriteLine("[DEBUG INFO] File doesn't exist: " + e);
                }
                catch (Microsoft.Xna.Framework.Content.Pipeline.InvalidContentException e)
                {
                    Debug.WriteLine("[DEBUG INFO] Bad operation: " + e);
                }
            }
        }

        /// <summary>
        /// change function for worlds - updates current active world
        /// </summary>
        /// <param name="engine">Engine instance</param>
        public void change_current(Engine engine)
        {
            if (current == worlds.Count - 1)
                current = 0;
            else
                current++;

            engine.set_camera_offset(Vector2.Zero);          
        }
        /// <summary>
        /// Get current active world
        /// </summary>
        /// <returns>World object</returns>
        public World get_current()
        {
            return worlds.ElementAt(current).world;
        }
        /// <summary>
        /// Find the world by its name
        /// </summary>
        /// <param name="world_name">string name</param>
        /// <returns>World object</returns>
        public World get_world_by_name(String world_name)
        {
            foreach (WorldStruct w in worlds)
            {
                if (w.world.worldname == world_name)
                {
                    return w.world;
                }
            }
            return null;
        }
    }
}