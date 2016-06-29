using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
using System.IO;
using System.Configuration;

namespace FdCruncher
{
    class Program
    {
        //acceptable total salary range for a selected team
        static readonly int MaxSalary = Convert.ToInt32(ConfigurationManager.AppSettings.Get("MaxSalary"));
        static readonly int MinSalary = Convert.ToInt32(ConfigurationManager.AppSettings.Get("MinSalary"));

        //total players from each position to use. Filtered based on value from data file
        static readonly int PositionPlayerPool = Convert.ToInt32(ConfigurationManager.AppSettings.Get("PositionPlayerPool"));
        static readonly int CenterPlayerPool = Convert.ToInt32(ConfigurationManager.AppSettings.Get("CenterPlayerPool"));

        static readonly string InputFile = ConfigurationManager.AppSettings.Get("InputFile");
        static readonly string OutputFile = ConfigurationManager.AppSettings.Get("OutputFile");

        //number of teams to pick
        static readonly int NumberOfTeams = Convert.ToInt32(ConfigurationManager.AppSettings.Get("NumberOfTeams"));

        //ranges used to short circuit permutation if a valid team is mathematically impossible at each step of the permutation
        static int Level1low = 0;
        static int Level2low = 0;
        static int Level3low = 0;
        static int Level1high = 0;
        static int Level2high = 0;
        static int Level3high = 0;
        static double Level4MaxPoints = 0;
        //stores the valid picks
        public static DraftTeam[] allValidPicks = new DraftTeam[NumberOfTeams];
        //Used to track the lowest value in the allValidPicks array so we know if we have to shuffle
        static double lowMan = 0;

        static void Main(string[] args)
        {
            //Initialize Teams
            allValidPicks = new DraftTeam[NumberOfTeams];
            for (int teamIndex = 0; teamIndex < NumberOfTeams; teamIndex++)
            {
                allValidPicks[teamIndex] = new DraftTeam();
            }

            IEnumerable<Player> todaysPlayers = GetTodaysPlayers();

            RunPermuations(todaysPlayers);

            OutputTeams();
        }

        /// <summary>
        /// Read Flat file and import today's players
        /// </summary>
        /// <returns>List of players in today's games, and their weighted expected production</returns>
        static List<Player> GetTodaysPlayers()
        {
            List<Player> players = new List<Player>();
            TextFieldParser parser = new TextFieldParser(new StreamReader(InputFile));
            parser.SetDelimiters("\t");
            parser.TextFieldType = FieldType.Delimited;
            string[] fieldNames = parser.ReadFields();
            while (!parser.EndOfData)
            {
                string[] fields = parser.ReadFields();
                players.Add(new Player
                {
                    ID = Convert.ToInt32(fields[0]),
                    Position = Convert.ToInt32(fields[1]),
                    Name = fields[2],
                    Salary = Convert.ToInt32(fields[3]),
                    Opponent = fields[4],
                    Team = fields[5],
                    Venue = fields[6],
                    SeasonAvgMins = Convert.ToDouble(fields[8]),
                    Metric = Convert.ToDouble(fields[9]),
                    Modifier = Convert.ToDouble(fields[10]),
                    Value = Convert.ToDouble(fields[11]),
                    PositionRank = Convert.ToInt32(fields[15]),
                    Include = fields[16] == "1",
                    Exclude = fields[17] == "1",
                });
            }

            return players;
        }

        /// <summary>
        /// Create tuple objects
        /// </summary>
        /// <param name="data">List of Players from Modified Output</param>
        static void RunPermuations(IEnumerable<Player> data)
        {
            IEnumerable<Player> p5s = data.Where(d => d.Position == 5 && d.Salary > 0 && !d.Exclude).OrderByDescending(d => d.Value * d.Modifier).Take(CenterPlayerPool);
            IEnumerable<Player> p4s = data.Where(d => d.Position == 4 && d.Salary > 0 && !d.Exclude).OrderByDescending(d => d.Value * d.Modifier).Take(PositionPlayerPool);
            IEnumerable<Player> p3s = data.Where(d => d.Position == 3 && d.Salary > 0 && !d.Exclude).OrderByDescending(d => d.Value * d.Modifier).Take(PositionPlayerPool);
            IEnumerable<Player> p2s = data.Where(d => d.Position == 2 && d.Salary > 0 && !d.Exclude).OrderByDescending(d => d.Value * d.Modifier).Take(PositionPlayerPool);
            IEnumerable<Player> p1s = data.Where(d => d.Position == 1 && d.Salary > 0 && !d.Exclude).OrderByDescending(d => d.Value * d.Modifier).Take(PositionPlayerPool);


            IList<Tuple<Player, Player>> p4permsT = GetPositionPerms(p4s, 4);
            IList<Tuple<Player, Player>> p3permsT = GetPositionPerms(p3s, 3);
            IList<Tuple<Player, Player>> p2permsT = GetPositionPerms(p2s, 2);
            IList<Tuple<Player, Player>> p1permsT = GetPositionPerms(p1s, 1);

            ComputeRanges(p5s, p4s, p3s, p2s, p1s);

            if (p5s.Where(d => d.Position == 5 && d.Include).Count() == 1)
            {
                PerformPermuation(p5s.Where(d => d.Position == 5 && d.Include).Single(), p1permsT.ToArray(), p2permsT.ToArray(), p3permsT.ToArray(), p4permsT.ToArray());
            }
            else
            {
                foreach (Player p5 in p5s)
                {
                    Console.WriteLine("Performing Permutation for Center: " + p5.Name + " " + DateTime.Now.ToString());
                    PerformPermuation(p5, p1permsT.ToArray(), p2permsT.ToArray(), p3permsT.ToArray(), p4permsT.ToArray());
                }
            }
        }

        /// <summary>
        /// Called against each position 1,2,3,4 to compute permutations with exclusions
        /// </summary>
        /// <param name="players">all players from that position we want to look at</param>
        /// <param name="includes">list of includedplayers we need to have</param>
        /// <param name="position">the int of the position this is for</param>
        /// <returns>2 positional player tuple representing a "posibility" for that position</returns>
        static IList<Tuple<Player, Player>> GetPositionPerms(IEnumerable<Player> players, int position)
        {
            IEnumerable<Player> includes = players.Where(x => x.Include);
            IList<Tuple<Player, Player>> perms = new List<Tuple<Player, Player>>();
            foreach (Player playerA in players)
            {
                foreach (Player playerB in players.Where(p => p.PositionRank > playerA.PositionRank))
                {
                    switch (includes.Where(i => i.Position == position).Count())
                    {
                        case 0:
                            perms.Add(new Tuple<Player, Player>(playerA, playerB));
                            break;
                        case 1:
                            string playerName = includes.Where(i => i.Position == position).Select(i => i.Name).Single();
                            if (playerName == playerA.Name || playerName == playerB.Name)
                            {
                                perms.Add(new Tuple<Player, Player>(playerA, playerB));
                            }
                            break;
                        case 2:
                            Player[] includedPlayers = includes.Where(i => i.Position == position).ToArray();
                            perms.Add(new Tuple<Player, Player>(includedPlayers[0], includedPlayers[1]));
                            break;
                        default:
                            throw new Exception("You have included too many players in a single position.");
                    }
                }
            }
            return perms;
        }

        /// <summary>
        /// Determine cutoffs for each permutation level if our salary and POINTS is mathematically impossible to result in a good team
        /// </summary>
        static void ComputeRanges(IEnumerable<Player> p5s, IEnumerable<Player> p4s, IEnumerable<Player> p3s, IEnumerable<Player> p2s, IEnumerable<Player> p1s)
        {
            int max4Salary = p4s.OrderByDescending(d => d.Salary).Take(2).Select(d => d.Salary).Sum();
            int min4Salary = p4s.OrderBy(d => d.Salary).Take(2).Select(d => d.Salary).Sum();
            int max3Salary = p3s.OrderByDescending(d => d.Salary).Take(2).Select(d => d.Salary).Sum();
            int min3Salary = p3s.OrderBy(d => d.Salary).Take(2).Select(d => d.Salary).Sum();
            int max2Salary = p2s.OrderByDescending(d => d.Salary).Take(2).Select(d => d.Salary).Sum();
            int min2Salary = p2s.OrderBy(d => d.Salary).Take(2).Select(d => d.Salary).Sum();

            Level3low = MinSalary - max4Salary;
            Level3high = MaxSalary - min4Salary;
            Level2low = MinSalary - max3Salary - max4Salary;
            Level2high = MaxSalary - min3Salary - min4Salary;
            Level1low = MinSalary - max2Salary - max3Salary - max4Salary;
            Level1high = MaxSalary - min2Salary - min3Salary - min4Salary;

            Level4MaxPoints = p4s.OrderByDescending(d => d.ModifiedMetric).Take(2).Select(d => d.ModifiedMetric).Sum();
        }

        /// <summary>
        /// For each center crunch the best teams
        /// </summary>
        static void PerformPermuation(Player p5, Tuple<Player, Player>[] p1s, Tuple<Player, Player>[] p2s, Tuple<Player, Player>[] p3s, Tuple<Player, Player>[] p4s)
        {
            int oneCount = p1s.Count();
            int twoCount = p2s.Count();
            int threeCount = p3s.Count();
            int fourCount = p4s.Count();
            double teamTotal1 = 0;
            double teamTotal2 = 0;
            double teamTotal3 = 0;
            double teamTotal = 0;

            for (int index1 = 0; index1 < oneCount; index1++)
            {
                Player p1a = p1s[index1].Item1;
                Player p1b = p1s[index1].Item2;
                teamTotal1 = p1a.Salary + p1b.Salary + p5.Salary;

                if ((teamTotal1 < Level1low || teamTotal1 > Level1high) && teamTotal1 != 0)
                    continue;
                for (int index2 = 0; index2 < twoCount; index2++)
                {
                    Player p2a = p2s[index2].Item1;
                    Player p2b = p2s[index2].Item2;
                    teamTotal2 = teamTotal1 + p2a.Salary + p2b.Salary;
                    if ((teamTotal2 < Level2low || teamTotal2 > Level2high) && teamTotal2 != 0)
                        continue;
                    for (int index3 = 0; index3 < threeCount; index3++)
                    {
                        Player p3a = p3s[index3].Item1;
                        Player p3b = p3s[index3].Item2;
                        teamTotal3 = teamTotal2 + p3a.Salary + p3b.Salary;

                        if ((teamTotal3 < Level3low || teamTotal3 > Level3high) && teamTotal3 != 0)
                            continue;

                        double totalPointsCheck = p1a.ModifiedMetric + p1b.ModifiedMetric + p2a.ModifiedMetric + p2b.ModifiedMetric + p3a.ModifiedMetric + p3b.ModifiedMetric + p5.ModifiedMetric;
                        if (totalPointsCheck + Level4MaxPoints < lowMan)
                            continue;

                        for (int index4 = 0; index4 < fourCount; index4++)
                        {

                            teamTotal = teamTotal3 + p4s[index4].Item1.Salary + p4s[index4].Item2.Salary;
                            if (teamTotal != 0 && (teamTotal > MaxSalary || teamTotal < MinSalary))
                                continue;

                            Player p4a = p4s[index4].Item1;
                            Player p4b = p4s[index4].Item2;
                            double totalPoints = totalPointsCheck + p4a.ModifiedMetric + p4b.ModifiedMetric;
                            if (totalPoints < lowMan && allValidPicks[NumberOfTeams - 1] != null)
                                continue;


                            for (int teamIndex = 0; teamIndex < NumberOfTeams; teamIndex++)
                            {
                                if (allValidPicks[teamIndex].TotalPoints < totalPoints)
                                {
                                    for (int shuffleIndex = NumberOfTeams - 2; shuffleIndex > teamIndex; shuffleIndex--)
                                    {
                                        allValidPicks[shuffleIndex + 1] = allValidPicks[shuffleIndex];
                                    }
                                    allValidPicks[teamIndex] = new DraftTeam
                                    {
                                        Team = new List<Player>
                                        {
                                            p1a, p1b, p2a, p2b, p3a, p3b, p4a, p4b, p5
                                        },
                                        TotalPoints = totalPoints
                                    };
                                    lowMan = allValidPicks[NumberOfTeams - 1] == null ? 0 : allValidPicks[NumberOfTeams - 1].TotalPoints;
                                    break;
                                }
                                else
                                    continue;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Output teams to text file in human readable format
        /// </summary>
        static void OutputTeams()
        {
            StreamWriter writer = new StreamWriter(OutputFile);
            int teamIndex = 0;
            foreach (DraftTeam team in allValidPicks)
            {
                writer.WriteLine("*********************************");
                writer.WriteLine("Team " + (teamIndex + 1) + " Expected Points: " + team.TotalPoints.ToString());
                foreach (Player player in team.Team)
                {
                    writer.WriteLine(player.Name + " " + player.Team + " " + player.Position + " " + player.ModifiedMetric.ToString());
                }
                teamIndex++;
            }
            writer.Close();

            Console.WriteLine("Finished...your file can be found here: " + OutputFile);
            Console.WriteLine("Press enter to continue...");
            Console.ReadLine();         
        }
    }
}
