using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using System.Timers;

namespace Spawn {
    [ApiVersion(1, 16)]
    public class BringToSpawn : TerrariaPlugin {
        Timer bringTimer = new Timer();
        Random r = new Random();
        uint time = 0;
        uint interval = 0;

        public BringToSpawn(Main game) : base(game) {
        }
        public override void Initialize() {
            bringTimer.Interval = 30000;
            bringTimer.AutoReset = true;
            bringTimer.Elapsed += new ElapsedEventHandler(bringAtInterval);
            Commands.ChatCommands.Add(new Command("tshock.bringtospawn", bringCmd, "bringtospawn")
            {
                HelpText = "Brings all players to spawn. You can also do /bringtospawn start <interval (minutes)> to set an interval and /bringtospawn end to end it."
            });
        }
        public override Version Version {
            get { return new Version("1.0"); }
        }
        public override string Name {
            get { return "Bring to Spawn"; }
        }
        public override string Author {
            get { return "GameRoom"; }
        }
        public override string Description {
            get { return "Brings all players, separated by team, to spawn."; }
        }

        void bringCmd(CommandArgs e) {
            if (e.Parameters.Count == 0) Bring();
            else if (e.Parameters[0] == "start") {
                 try {
                    time = Convert.ToUInt32(e.Parameters[1]) * 2;
                    if (time < 1) e.Player.SendErrorMessage("The time parameter must be greater than zero.");
                    else {
                        interval = time;
                        bringTimer.Start();
                        string txt;
                        if (time == 1) txt = "Players will be teleported to the spawn every minute.";
                        else txt = String.Format("Players will be teleported to the spawn every {0} minutes.", e.Parameters[1]);
                        bc(txt);
                    }
                 }
                catch (FormatException) {
                    e.Player.SendErrorMessage("The time parameter must be a positive integer.");
                }
                catch (ArgumentOutOfRangeException) {
                    e.Player.SendErrorMessage("You must have a time parameter that is a positive integer.");
                }
            } else if (e.Parameters[0] == "end") {
                bringTimer.Stop();
                bc("The teleport timer has stopped.");
            }
        }

        public void bringAtInterval(object source, ElapsedEventArgs e) {
            time--;
            if (time <= 0) {
                time = interval;
                Bring();
            }
            if (time == 1)
                bc("All players will be teleported to spawn in thirty seconds.");
            if (time == 2)
                bc("All players will be teleported to spawn in one minute.");
        }

        void Bring() {
            //player coords are in top left
            List<bringPos> positions = new List<bringPos>();
            foreach (TSPlayer player in TShock.Players)
                if (player != null && player.Active && !player.Dead)
                    if (!positions.Exists(x => x.Team == player.Team) || player.Team == 0) {
                        bringPos playerGroup = new bringPos {
                            players = new List<int> { player.Index },
                            Team = player.Team
                        };
                        positions.Add(playerGroup);
                    } else positions.Find(x => x.Team == player.Team).players.Add(player.Index);
            List<bringPos> randomList = new List<bringPos>();
            int randomIndex = 0;
            while (positions.Count > 0) {
                randomIndex = r.Next(0, positions.Count - 1); //Choose a random object in the list
                randomList.Add(positions[randomIndex]); //add it to the new, random list
                positions.RemoveAt(randomIndex); //remove to avoid duplicates
            }
            int posX = Main.spawnTileX - 6 * (randomList.Count - 1);
            foreach(bringPos pos in randomList) {
                pos.X = posX + r.Next(-2, 2);
                posX += 12;
                pos.Y = Main.spawnTileY - 3;
                while (true) {
                    bool canBreak = true;
                    for (byte xx = 0; xx < 2; xx++)
                        for (byte yy = 0; yy < 3; yy++) {
                            Tile Tile = Main.tile[pos.X + xx, pos.Y + yy];
                            if (Tile.collisionType > 0 || Tile.lava()) canBreak = false;
                            }
                    if (canBreak) break;
                    else pos.Y--;
                }
                while (true) {
                    bool canBreak = false;
                    for (byte xx = 0; xx < 2; xx++) {
                        Tile Tile = Main.tile[pos.X + xx, pos.Y + 3];
                        if (Tile.collisionType > 0) canBreak = true;
                    }
                    if (canBreak) break;
                    else pos.Y++;
                }
                foreach (int plr in pos.players)
                    TShock.Players[plr].Teleport(pos.X * 16, pos.Y * 16);
            }
            bc("All players have been brought to spawn.");
        }

        void bc(string text) {
            TShock.Utils.Broadcast(text, Convert.ToByte(TShock.Config.BroadcastRGB[0]), Convert.ToByte(TShock.Config.BroadcastRGB[1]), Convert.ToByte(TShock.Config.BroadcastRGB[2]));
        }
    }

    public class bringPos {
        public int X { get; set; }
        public int Y { get; set; }
        public int Team { get; set; }
        public List<int> players { get; set; } //= new List<int>();
    }
}