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
        private async void button1_Click(object sender, EventArgs e)
        {
            //listProf _listi_ = new listProf();
            List<string> listProf = new List<string>();

            IRestRequest request = new RestRequest(string.Format("{0}?page={1}&per_page={2}&salary={3}&only_with_salary=true", HhApiVacanciesResource, VacanciesFirstPage, VacanciesPerPage, bigSalary), Method.GET);
            IRestResponse response = _client.Execute(request);    //RequestVacancies(VacanciesFirstPage);
            int pagesCount = (int)JObject.Parse(response.Content)["pages"];
            JArray vacancies = JObject.Parse(response.Content)["items"] as JArray;
            for (int i = VacanciesFirstPage; i < pagesCount; i++)
            {
                foreach (JToken vacancy in vacancies)
                {                                      
                    JToken salaryCurr = vacancy["salary"]["currency"];

                    double salary = 0;
                    if ((string)salaryCurr != "RUR")
                        salary = translate(vacancy);
                    else
                        salary = sumSalary(vacancy);

                    if (salary > bigSalary)
                        listProf.Add((string)vacancy["name"]);

                }
                int FirstPage = VacanciesFirstPage + i;
                request = new RestRequest(string.Format("{0}?page={1}&per_page={2}&salary={3}&only_with_salary=true", HhApiVacanciesResource, FirstPage, VacanciesPerPage, bigSalary), Method.GET);
                response = _client.Execute(request);
                vacancies = JObject.Parse(response.Content)["items"] as JArray;
            }

            listBox1.DataSource = listProf;


            /*
            using (HttpClient client = new HttpClient())
            {
                // Call asynchronous network methods in a try/catch block to handle exceptions
                try
                {
                    HttpResponseMessage response = await client.GetAsync("https://api.hh.ru/vacancies/?page=0&per_page=100&only_with_salary=true&salary=120000");
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    // Above three lines can be replaced with new helper method below
                    // string responseBody = await client.GetStringAsync(uri);

                    Console.WriteLine(responseBody);
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", ex.Message);
                }
                }
     * */


        }

        private async void button2_Click(object sender, EventArgs e)
        {
            List<string> listProf = new List<string>(); 

            IRestRequest request = new RestRequest(string.Format("{0}?page={1}&per_page={2}&salary={3}&only_with_salary=true", HhApiVacanciesResource, VacanciesFirstPage, VacanciesPerPage, bigSalary), Method.GET);
            IRestResponse response = _client.Execute(request);    //RequestVacancies(VacanciesFirstPage);
            int pagesCount = (int)JObject.Parse(response.Content)["pages"];
            JArray vacancies = JObject.Parse(response.Content)["items"] as JArray;
            for (int i = VacanciesFirstPage; i < pagesCount; i++)
            {
                foreach (JToken vacancy in vacancies)
                {
                    JToken salaryCurr = vacancy["salary"]["currency"];

                    double salary = 0;
                    if ((string)salaryCurr != "RUR")
                        salary = translate(vacancy);
                    else
                        salary = sumSalary(vacancy);

                    if (salary > bigSalary)
                    {
                        IRestRequest request_details = new RestRequest(string.Format("{0}/{1}", HhApiVacanciesResource, (string)vacancy["id"]), Method.GET);
                        IRestResponse response_details = _client.Execute(request_details);

                        JToken vacancyDetails = JObject.Parse(response_details.Content); //     (RequestVacancyDetails((string)vacancy["id"]).Content);
                        JArray keySkills = vacancyDetails["key_skills"] as JArray;
                        if (keySkills.HasValues)
                        {
                            foreach (JToken keySkill in keySkills)
                            {
                                listProf.Add((string)keySkill["name"]);
                            }
                        }
                        else
                            continue;
                    }

                   


                }
                int FirstPage = VacanciesFirstPage + i;
                request = new RestRequest(string.Format("{0}?page={1}&per_page={2}&salary={3}&only_with_salary=true", HhApiVacanciesResource, FirstPage, VacanciesPerPage, bigSalary), Method.GET);
                response = _client.Execute(request);
                vacancies = JObject.Parse(response.Content)["items"] as JArray;
            }

            listBox2.DataSource = listProf;
        }

       
    }

}

