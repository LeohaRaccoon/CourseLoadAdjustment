using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;

namespace CourseLoadAdjustment
{
    public partial class MainWindow : Window
    {
        private string connectionString = "Server=RACCOONSHIP;Database=UniversityDB;Integrated Security=True;";
        private int selectedFiredTeacherId;
        private List<int> selectedCourses = new List<int>();

        public MainWindow()
        {
            InitializeComponent();
            LoadFiredTeachers();
        }

        private void LoadFiredTeachers()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT TeacherID, TeacherName FROM Teachers WHERE StatusID = 2"; // Уволенные
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ComboBoxFiredTeacher.Items.Add(new Teacher
                            {
                                TeacherID = reader.GetInt32(0),
                                TeacherName = reader.GetString(1)
                            });
                        }
                    }
                }
            }
        }

        private void GetCourses_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBoxFiredTeacher.SelectedItem is Teacher selectedTeacher)
            {
                selectedFiredTeacherId = selectedTeacher.TeacherID;
                LoadCourses(selectedFiredTeacherId);
                LoadAvailableTeachers(selectedFiredTeacherId);
            }
        }

        private void LoadCourses(int teacherId)
        {
            ListBoxCourses.Items.Clear();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
            SELECT c.CourseName 
            FROM TeacherCourses tc
            JOIN Courses c ON tc.CourseID = c.CourseID
            WHERE tc.TeacherID = @TeacherID";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TeacherID", teacherId);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ListBoxCourses.Items.Add(reader.GetString(0)); // Добавляем название курса
                        }
                    }
                }
            }
        }


        private void LoadAvailableTeachers(int firedTeacherId)
        {
            ListBoxAvailableTeachers.Items.Clear();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT t.TeacherID, t.TeacherName 
                    FROM Teachers t 
                    WHERE t.StatusID = 1 AND t.TeacherID NOT IN (
                        SELECT TeacherID FROM TeacherCourses WHERE CourseID IN (
                            SELECT CourseID FROM TeacherCourses WHERE TeacherID = @FiredTeacherID
                        )
                    )";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FiredTeacherID", firedTeacherId);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ListBoxAvailableTeachers.Items.Add(new Teacher
                            {
                                TeacherID = reader.GetInt32(0),
                                TeacherName = reader.GetString(1)
                            });
                        }
                    }
                }
            }
        }

        private void DistributeCourses_Click(object sender, RoutedEventArgs e)
        {
            selectedCourses.Clear();
            foreach (var selectedItem in ListBoxCourses.SelectedItems)
            {
                // Здесь мы не сохраняем идентификаторы курсов, а просто показываем названия
                string courseName = (string)selectedItem;
                MessageBox.Show($"Курс '{courseName}' будет распределен.");
            }

            if (ListBoxAvailableTeachers.SelectedItem is Teacher selectedTeacher)
            {
                // Здесь можно добавить логику для временного распределения курсов
                MessageBox.Show($"Курсы будут распределены преподавателю {selectedTeacher.TeacherName}.");
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите доступного преподавателя для распределения курсов.");
            }
        }


        private void ConfirmChanges_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxAvailableTeachers.SelectedItem is Teacher selectedTeacher)
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    foreach (var selectedItem in ListBoxCourses.SelectedItems)
                    {
                        string courseName = (string)selectedItem;

                        // Получаем CourseID по названию курса
                        string query = "SELECT CourseID FROM Courses WHERE CourseName = @CourseName";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@CourseName", courseName);
                            int courseId = (int)command.ExecuteScalar(); // Получаем CourseID

                            // Вставляем курс в TeacherCourses
                            string insertQuery = "INSERT INTO TeacherCourses (TeacherID, CourseID) VALUES (@TeacherID, @CourseID)";
                            using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                            {
                                insertCommand.Parameters.AddWithValue("@TeacherID", selectedTeacher.TeacherID);
                                insertCommand.Parameters.AddWithValue("@CourseID", courseId);
                                insertCommand.ExecuteNonQuery();
                            }
                        }
                    }
                }
                MessageBox.Show("Изменения успешно подтверждены!");
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите доступного преподавателя для подтверждения изменений.");
            }
        }

    }

    public class Teacher
    {
        public int TeacherID { get; set; }
        public string TeacherName { get; set; }

        public override string ToString()
        {
            return TeacherName; // Для отображения в ComboBox и ListBox
        }
    }
}