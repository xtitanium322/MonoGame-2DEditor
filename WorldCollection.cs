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
/*
 * Every world in the game + functions that let player interact with these worlds
 */
namespace EditorEngine
{
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
    //**************************//
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
        // add world to world list
        public void add_world(String filename, World w)
        {
            if (worlds.Count == 0)
                current = 0;

            worlds.Add(new WorldStruct(filename, w));
        }
        // load xml file into the game
        public void load_tiles(Engine engine)
        {
            foreach (WorldStruct w in worlds)
            {
                ArrayList tile_map = new ArrayList();

                try
                {

                    using (FileStream stream = new FileStream(w.filename, FileMode.Open)) //open xml file, close??
                    {
                        using (XmlReader reader = XmlReader.Create(stream)) // open file in xml reader
                        {

                            tile_map = IntermediateSerializer.Deserialize<ArrayList>(reader, null);
                            //tile_map = tiles.Cast<MapData>().ToList(); // load into the list
                        }
                    }
                    // generating map ui_elements
                    // optimization - remove foreach loop and only calculate range once - change of format to ArrayList
                        //foreach (MapData tile_info in tile_map)
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
                                    w.world.generate_tile(Tile.find_tile_id(map_object.block_name),
                                    map_object.block_x + x,
                                    map_object.block_y + y,
                                    engine);
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
            }
        }
        // change function for 2 worlds in the list
        public void change_current(Engine engine, String world_name)
        {
            if (current == 0)
                current = 1;
            else
                current = 0;

            engine.set_camera_offset(Vector2.Zero);
        }

        public World get_current()
        {
            return worlds.ElementAt(current).world;
        }

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