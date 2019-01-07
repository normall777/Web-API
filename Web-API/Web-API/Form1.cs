﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RestSharp;
using Newtonsoft.Json.Linq;

namespace Web_API
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// Переменные для запроса
        /// </summary>
        const string HhApiHost = "https://api.hh.ru"; //Куда
        const string HhApiVacanciesResource = "/vacancies"; //Что
        const int VacanciesPerPage = 100; //Число вакансий на странице (500 не работает)
        const int VacanciesFirstPage = 0; //Начать с первой страницы
        const int bigSalary = 120000;
        const int lowSalary = 15000;
        readonly IRestClient _client = new RestClient(HhApiHost);
        private JArray curr;
        

        public Form1()
        {

            /// <summary>
            /// Для загрузки значения для перевода валют
            /// </summary>
            IRestRequest request = new RestRequest(string.Format("https://api.hh.ru/dictionaries"), Method.GET);
            IRestResponse response = _client.Execute(request);
            curr = JObject.Parse(response.Content)["currency"] as JArray;


            InitializeComponent();
        }
        /// <summary>
        /// Функция перевода иностранной валюты в RUR
        /// </summary>
        /// <param name="vacancy"></param>
        /// <returns></returns>
        private double translate(JToken vacancy)
        {
            JToken cur = vacancy["salary"]["currency"];
            double salary = 0;
            foreach (JToken _curr in curr)
            {
                if(cur == _curr["code"])
                {
                    return salary = sumSalary(vacancy, (double)_curr["rate"]);           
                }
            }
            return salary;
        }
        /// <summary>
        /// Функция для получения "фактического" значения зарплаты
        /// </summary>
        /// <param name="vacancy"></param>
        /// <param name="rate"></param>
        /// <returns></returns>
        private double sumSalary (JToken vacancy, double rate = 1)
        {
            double salary = 0;
            JToken salaryFrom = vacancy["salary"]["from"];
            JToken salaryTo = vacancy["salary"]["to"];
            JTokenType salaryFromType = salaryFrom.Type;
            JTokenType salaryToType = salaryTo.Type;
            if (salaryFromType != JTokenType.Null && salaryToType != JTokenType.Null)
                salary = (((double)salaryFrom + (double)salaryTo) / 2) / rate;
            else if (salaryFromType == JTokenType.Null && salaryToType != JTokenType.Null)
                salary = (double)salaryTo / rate;
            else if (salaryFromType != JTokenType.Null && salaryToType == JTokenType.Null)
                salary = (double)salaryFrom / rate;
            return salary;
        }
        /// <summary>
        /// Функция для выполнения API-запросов
        /// </summary>
        /// <param name="big_salary"></param> Флаг для поиска 120000
        /// <param name="details"></param> Флаг для поиска ключевых навыков
        /// <returns></returns>
        private List<string> RABOTAY_BLE(bool big_salary, bool details)
        {
            IRestRequest request;
            int pagesCount = 1;
            int FirstPage = 0;
            List<string> listProf = new List<string>();
            do
            {
                if (big_salary)
                {
                    request = new RestRequest(string.Format("{0}?page={1}&per_page={2}&salary={3}&only_with_salary=true", HhApiVacanciesResource, FirstPage, VacanciesPerPage, bigSalary), Method.GET);
                }
                else
                {
                    request = new RestRequest(string.Format("{0}?page={1}&per_page={2}&only_with_salary=true", HhApiVacanciesResource, FirstPage, VacanciesPerPage), Method.GET);
                }
                IRestResponse response = _client.Execute(request);
                pagesCount = (int)JObject.Parse(response.Content)["pages"];
                JArray vacancies = JObject.Parse(response.Content)["items"] as JArray;
                foreach (JToken vacancy in vacancies)
                {
                    JToken salaryCurr = vacancy["salary"]["currency"];

                    double salary = 0;
                    if ((string)salaryCurr != "RUR")
                        salary = translate(vacancy);
                    else
                        salary = sumSalary(vacancy);
                    if (!details)
                    {

                        if (big_salary && salary >= bigSalary)
                            listProf.Add((string)vacancy["name"]);
                        else if (!big_salary && salary < lowSalary && salary > 0)
                            listProf.Add((string)vacancy["name"]);
                    }
                    else
                    {
                        if ((big_salary && salary >= bigSalary) || (!big_salary && salary < lowSalary && salary > 0))
                        {
                            IRestRequest request_details = new RestRequest(string.Format("{0}/{1}", HhApiVacanciesResource, (string)vacancy["id"]), Method.GET);
                            IRestResponse response_details = _client.Execute(request_details);

                            JToken vacancyDetails = JObject.Parse(response_details.Content);
                            JArray keySkills = vacancyDetails["key_skills"] as JArray;
                            if (keySkills.HasValues)
                            {
                                foreach (JToken keySkill in keySkills)
                                {
                                    listProf.Add((string)keySkill["name"]);
                                }
                            }
                        }
                    }
                }
                FirstPage += 1;
            } while (FirstPage < pagesCount);
            return listProf;
        }
        /// <summary>
        /// Функция выводы результатов на форму
        /// </summary>
        /// <param name="data"></param> Набор данных для вывода
        /// <param name="listBox"></param> Окно (listbox) для вывода
        /// <param name="labelCount"></param> Параметр чилса найденных результатов
        /// <param name="labelCountClear"></param> Число найденных различающихся результатов
        private void ShowOnForm(List<string> data, ListBox listBox, Label labelCount, Label labelCountClear)
        {
            labelCount.Text = data.Count.ToString();
            data = data.Distinct().ToList();
            labelCountClear.Text = data.Count.ToString();
            listBox.DataSource = data;
        }
        /// <summary>
        /// Вызов функций при нажатии на кнопки:
        /// вызов функции запроса
        /// и
        /// вызов функции отображения
        /// </summary>
        private async void button1_Click(object sender, EventArgs e)
        {
            List<string> data = RABOTAY_BLE(true, false).ToList();
            ShowOnForm(data, listBox1, labelCount1, labelCountClear1);          
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            List<string> data = RABOTAY_BLE(true,true).Distinct().ToList();
            ShowOnForm(data, listBox2, labelCount2, labelCountClear2);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            List<string> data = RABOTAY_BLE(false,false).Distinct().ToList();
            ShowOnForm(data, listBox3, labelCount3, labelCountClear3);
            }

        private void button4_Click(object sender, EventArgs e)
        {
            List<string> data = RABOTAY_BLE(false, true).Distinct().ToList();
            ShowOnForm(data, listBox4, labelCount4, labelCountClear4);           
        }
    }

}

