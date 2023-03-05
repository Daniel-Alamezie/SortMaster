using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SortMaster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        public List<Int64> userInputList = new List<Int64>();

        public MainWindow()
        {
            InitializeComponent();
            CreatTable();

        }


        //Compute when button is hit
        private void ComputeButton_Click(object sender, RoutedEventArgs e)
        {
            // Check if the user input is a valid string
            string input = NumberOfNumbersTextBox.Text;
            if (string.IsNullOrWhiteSpace(input))
            {
                MessageBox.Show("Please enter some numbers.");
                return;
            }

            // Validate that only integers are accepted as input
            int result;
            bool isNumeric = int.TryParse(input, out result);
            if (!isNumeric)
            {
                MessageBox.Show("Please enter only integer numbers.");
                return;
            }


            // Convert the user input string into a list of integers
            List<Int64> userInputList = input.Select(x => (Int64)Char.GetNumericValue(x)).ToList();

            // Get the selected sort order from the ComboBox
            ComboBoxItem selectedSortOrder = SortOrderComboBox.SelectedItem as ComboBoxItem;
            string sortOrder = selectedSortOrder.Content.ToString();

            // Sort the user input list
            List<Int64> sortedList;
            TimeSpan timeTaken = new TimeSpan();

            //Apply sorting based on users selection
            if (sortOrder == "Ascending")
            {
                DateTime startTime = DateTime.Now;
                QuickSort(userInputList, 0, userInputList.Count - 1, sortOrder);
                sortedList = userInputList;
                DateTime endTime = DateTime.Now;
                timeTaken = endTime - startTime;
            }
            else
            {
                DateTime startTime = DateTime.Now;
                sortedList = userInputList.OrderByDescending(x => x).ToList();
                DateTime endTime = DateTime.Now;
                timeTaken = endTime - startTime;
            }


            // Display the user input, sorted numbers, sort order, and time taken in the feedback section
            UserInputTextBlock.Text = NumberOfNumbersTextBox.Text;
            SortOrderTextBlock.Text = sortOrder;
            SortedNumbersTextBlock.Text = string.Join(", ", sortedList);
            TimeTakenTextBlock.Text = timeTaken.TotalMilliseconds + " milliseconds";
            Status.Text = "Task Complete";

            //insert into the db here
            InsertData(NumberOfNumbersTextBox.Text, string.Join(", ", sortedList), sortOrder, timeTaken.TotalMilliseconds, Status.Text);

        }



        //Quick sort algorithm to sort the users input
        private void QuickSort(List<Int64> list, int left, int right, string sortOrder)
        {
            if (left < right)
            {
                int pivotIndex = Partition(list, left, right, sortOrder);

                QuickSort(list, left, pivotIndex - 1, sortOrder);
                QuickSort(list, pivotIndex + 1, right, sortOrder);
            }
        }

        // goes through each partition repeating the swap process
        private int Partition(List<Int64> list, int left, int right, string sortOrder)
        {
            Int64 pivot = list[right];
            int i = left - 1;

            for (int j = left; j < right; j++)
            {
                //arrange in ascending order
                if (sortOrder == "Ascending")
                {
                    if (list[j] <= pivot)
                    {
                        i++;
                        Swap(list, i, j);
                    }
                }
                // else do it in descending order
                else
                {
                    if (list[j] >= pivot)
                    {
                        i++;
                        Swap(list, i, j);
                    }
                }
            }

            Swap(list, i + 1, right);
            return i + 1;
        }

        // logic to compare and swap integers
        private void Swap(List<Int64> list, int i, int j)
        {
            Int64 temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }


        //export db data to Json on click
        private void ExportToJSONButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToJoson();
        }

        private void CreatTable()
        {
            string fileName = "SortResult.db";
            string appPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string filePath = Path.Combine(appPath, fileName);

            // Connect to the SQLite database file
            using (SQLiteConnection connection = new SQLiteConnection($"Data Source={filePath};"))
            {
                connection.Open();

                // Check if the Sort_Results table already exists
                using (SQLiteCommand command = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table' AND name='Sort_Results';", connection))
                {
                    object result = command.ExecuteScalar();
                    if (result == null)
                    {
                        command.CommandText = @"CREATE TABLE IF NOT EXISTS Sort_Results (
                                        UserInput TEXT,
                                        SortedNumbers TEXT,
                                        TimeTaken INTEGER,
                                        SortOrder TEXT,
                                        Status TEXT
                                    );";
                        command.ExecuteNonQuery();

                    }
                    else
                    {
                        return;
                    }

                }

            }

        }


        //Insert data into the db
        private void InsertData(string userInput, string sortedNumbers, string sortOrder, double timeTaken, string status)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SortResult.db");

            using (SQLiteConnection connection = new SQLiteConnection($"Data Source={filePath}"))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = "INSERT INTO Sort_Results (UserInput, SortedNumbers, SortOrder, TimeTaken, Status) " +
                                      "VALUES (@UserInput, @SortedNumbers, @SortOrder, @TimeTaken, @Status)";
                    command.Parameters.AddWithValue("@UserInput", userInput);
                    command.Parameters.AddWithValue("@SortedNumbers", sortedNumbers);
                    command.Parameters.AddWithValue("@SortOrder", sortOrder);
                    command.Parameters.AddWithValue("@TimeTaken", timeTaken);
                    command.Parameters.AddWithValue("@Status", status);

                    command.ExecuteNonQuery();
                }
            }
        }

        private void ExportToJoson()
        {
            string filePath = @"SortResult.json";
            using (SQLiteConnection connection = new SQLiteConnection("Data Source=SortResult.db"))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand("SELECT * FROM Sort_Results", connection))
                {
                    SQLiteDataReader reader = command.ExecuteReader();
                    List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
                    Dictionary<string, object> row;
                    while (reader.Read())
                    {
                        row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row.Add(reader.GetName(i), reader[i]);
                        }
                        rows.Add(row);
                    }

                    // Serialize the rows into a JSON string
                    string json = JsonConvert.SerializeObject(rows);

                    // Write the JSON string to the file
                    System.IO.File.WriteAllText(filePath, json);
                }
            }

        }

    }

}
