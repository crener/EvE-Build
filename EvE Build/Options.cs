﻿using System;
using System.Windows.Forms;
using System.IO;

namespace EvE_Build_UI
{
    public partial class Options : Form
    {
        bool updateOnStartup;
        string[] stationNames;
        int[] stationIds;
        int updateInterval = 1;

        public Options(string[] stationsName, int[] stationsIds, bool updateOnStartup, int update)
        {
            InitializeComponent();

            stationNames = stationsName;
            stationIds = stationsIds;
            this.updateOnStartup = updateOnStartup;
            updateInterval = update;

            setupGUI();
        }
        void setupGUI()
        {
            Station1Name.Text = stationNames[0];
            Station1ID.Text = stationIds[0].ToString();

            Station2Name.Text = stationNames[1];
            Station2ID.Text = stationIds[1].ToString();

            Station3Name.Text = stationNames[2];
            Station3ID.Text = stationIds[2].ToString();

            Station4Name.Text = stationNames[3];
            Station4ID.Text = stationIds[3].ToString();

            Station5Name.Text = stationNames[4];
            Station5ID.Text = stationIds[4].ToString();

            updateStartup.Checked = updateOnStartup;
            UpdateInvervalSelect.Value = updateInterval;
        }

        void Options_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            this.Dispose();
        }

        private void Station1Name_TextChanged(object sender, EventArgs e)
        {
            stationNames[0] = Station1Name.Text;
            save();
        }

        private void Station2Name_TextChanged(object sender, EventArgs e)
        {
            stationNames[1] = Station2Name.Text;
            save();
        }

        private void Station3Name_TextChanged(object sender, EventArgs e)
        {
            stationNames[2] = Station3Name.Text;
            save();
        }

        private void Station4Name_TextChanged(object sender, EventArgs e)
        {
            stationNames[3] = Station4Name.Text;
            save();
        }

        private void Station5Name_TextChanged(object sender, EventArgs e)
        {
            stationNames[4] = Station5Name.Text;
            save();
        }

        private void Station1ID_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (Station1ID.Text == null)
                {
                    stationIds[0] = 0;
                }
                else
                {
                    stationIds[0] = Int32.Parse(Station1ID.Text.ToString());
                }
                save();
            }
            catch (Exception)
            {
                Station1ID.Text = Station1ID.Text.ToString().Remove(Station1ID.Text.ToString().Length - 1);
            }
        }

        private void Station2ID_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (Station2ID.Text == null)
                {
                    stationIds[1] = 0;
                }
                else
                {
                    stationIds[1] = Int32.Parse(Station2ID.Text.ToString());
                }
                save();
            }
            catch (Exception)
            {

            }
        }

        private void Station3ID_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (Station3ID.Text == null)
                {
                    stationIds[2] = 0;
                }
                else
                {
                    stationIds[2] = Int32.Parse(Station3ID.Text.ToString());
                }
                save();
            }
            catch (Exception)
            {

            }
        }

        private void Station4ID_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (Station4ID.Text == null)
                {
                    stationIds[3] = 0;
                }
                else
                {
                    stationIds[3] = Int32.Parse(Station4ID.Text.ToString());
                }
                save();
            }
            catch (Exception)
            {

            }
        }

        private void Station5ID_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (Station5ID.Text == null)
                {
                    stationIds[4] = 0;
                }
                else
                {
                    stationIds[4] = Int32.Parse(Station5ID.Text.ToString());
                }
                    save();
            }
            catch (Exception)
            {

            }
        }

        private void updateStartup_CheckedChanged(object sender, EventArgs e)
        {
            updateOnStartup = updateStartup.Checked;
            save();
        }

        private void UpdateInvervalSelect_ValueChanged(object sender, EventArgs e)
        {
            updateInterval = Convert.ToInt32(UpdateInvervalSelect.Value);
            save();
        }

        private void save()
        {
            //start generating the default settings for a new file
            string file = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\EVE\\zz EvE-Build\\Settings.txt";
            StreamWriter newSettings = new StreamWriter(file);
            newSettings.WriteLine("Stations");

            for (int i = 0; i < stationNames.Length; ++i)
            {
                newSettings.WriteLine(stationNames[i] + "," + stationIds[i]);
            }

            newSettings.WriteLine("UpdateStart: " + updateOnStartup.ToString());
            newSettings.WriteLine("UpdateInterval: " + updateInterval);

            newSettings.Close();
        }
    }
}
