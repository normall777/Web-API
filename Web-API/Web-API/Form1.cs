using System;
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
        private JArray listOfCurrency;
        private List<JToken> listVacancyBigSalary = new List<JToken>();
        private List<JToken> listVacancyLowSalary = new List<JToken>();
        Form formWait = new FormWait();

        public Form1()
        {
            /// <summary>
            /// Для загрузки значений для перевода валют
            /// </summary>
            IRestRequest request = new RestRequest(string.Format("https://api.hh.ru/dictionaries"), Method.GET);
            IRestResponse response = _client.Execute(request);
            listOfCurrency = JObject.Parse(response.Content)["currency"] as JArray;

            InitializeComponent();
        }
        /// <summary>
        /// Функция возвращает коэффициент для перевода Salary в RUB
        /// </summary>
        /// <param name="currentCurrency">Валюта</param>
        /// <returns></returns>
        private double TranslateRate(JToken currentCurrency)
        {
            foreach (JToken curr in listOfCurrency)
            {
                if((string)currentCurrency == (string)curr["code"])
                {
                    return (double)curr["rate"];           
                }
            }
            throw new KeyNotFoundException();
        }
        /// <summary>
        /// Функция для получения "фактического" значения зарплаты
        /// </summary>
        /// <param name="vacancy"></param>
        /// <param name="rate"></param>
        /// <returns></returns>
        private double SumSalary (JToken vacancy, double rate = 1)
        {
            double salary = 0;
            JToken salaryFrom = vacancy["salary"]["from"];
            JToken salaryTo = vacancy["salary"]["to"];
            JTokenType salaryFromType = salaryFrom.Type;
            JTokenType salaryToType = salaryTo.Type;
            if (salaryFromType != JTokenType.Null && salaryToType != JTokenType.Null)
                salary = (((double)salaryFrom + (double)salaryTo) / 2) / rate; //Расчет среднего, если указано от и до
            else if (salaryFromType == JTokenType.Null && salaryToType != JTokenType.Null)
                salary = (double)salaryTo / rate; //Указано до
            else if (salaryFromType != JTokenType.Null && salaryToType == JTokenType.Null)
                salary = (double)salaryFrom / rate; //Указано от
            return salary;
        }
        /// <summary>
        /// Функция для выполнения API-запросов
        /// </summary>
        /// <param name="flagBigSalary">Флаг для поиска 120000</param> 
        /// <returns></returns>
        private List<JToken> GetVacancy(bool flagBigSalary)
        {
            formWait.Show(this);
            List<JToken> listVacancies = new List<JToken>();
            IRestRequest request;
            int pagesCount = 0;
            int FirstPage = 0;
            do
            {
                //Строится запрос
                if (flagBigSalary) // При указании salary сервис отдает предложения для такой суммы, без указания - все предложения
                { 
                    request = new RestRequest(string.Format("{0}?page={1}&per_page={2}&salary={3}&only_with_salary=true", HhApiVacanciesResource, FirstPage, VacanciesPerPage, bigSalary), Method.GET);
                }
                else
                {
                    request = new RestRequest(string.Format("{0}?page={1}&per_page={2}&only_with_salary=true", HhApiVacanciesResource, FirstPage, VacanciesPerPage), Method.GET);
                }
                IRestResponse response = _client.Execute(request);
                pagesCount = (int)JObject.Parse(response.Content)["pages"]; // Количество страниц всего
                JArray vacancies = JObject.Parse(response.Content)["items"] as JArray; //Получение массива вакансий
                foreach (JToken vacancy in vacancies) //Обработка каждой вакансии из массива
                {
                    JToken salaryCurr = vacancy["salary"]["currency"];//Считывание валюты
                    double rate = 1;
                    double salary = 0;
                    if ((string)salaryCurr != "RUR") //Если не рубли, то нужно перевести в рубли, т.е. посчитать коэффициент перевода rate
                    {
                        rate = TranslateRate(salaryCurr);
                    }

                    salary = SumSalary(vacancy, rate);
                    //Если подходящая salary, то добавить ее в список
                    if (flagBigSalary && salary >= bigSalary)
                        listVacancies.Add(vacancy);
                    else if (!flagBigSalary && salary < lowSalary && salary > 0)
                        listVacancies.Add(vacancy);
                }
                FirstPage += 1;
            } while (FirstPage < pagesCount);
            formWait.Hide();
            return listVacancies;
        }
        /// <summary>
        /// Функция получения названий вакансий
        /// </summary>
        /// <param name="vacancies">Список вакансий</param>
        /// <returns></returns>
        private List<string> GetVacanciesNames(List<JToken> vacancies)
        {
            List<string> listNames = new List<string>();
            foreach(JToken vacancy in vacancies)
            {
                listNames.Add((string)vacancy["name"]);
            }
            return listNames;
        }
        /// <summary>
        /// Функция получения ключевых навыков
        /// </summary>
        /// <param name="flagBigSalary">Флаг для поиска Salary больше 120000</param>
        /// <returns></returns>
        private List<string> GetVacanciesDetails(bool flagBigSalary)
        {
            List<JToken> vacancies = new List<JToken>();
            //Если вакансии не были раньше получены, получить их
            if (flagBigSalary == true && listVacancyBigSalary.Count == 0 || flagBigSalary==false && listVacancyLowSalary.Count==0)
            {
                vacancies = GetVacancy(flagBigSalary);
                if (flagBigSalary == true) listVacancyBigSalary = vacancies; else listVacancyLowSalary = vacancies;
            }
            formWait.Show(this);
            if (flagBigSalary == true) vacancies = listVacancyBigSalary; else vacancies = listVacancyLowSalary;
            
            //Составление списка ключевых навыков
            List<string> listDetails = new List<string>();
            foreach (JToken vacancy in vacancies)
            {
                IRestRequest request_details = new RestRequest(string.Format("{0}/{1}", HhApiVacanciesResource, (string)vacancy["id"]), Method.GET);
                IRestResponse response_details = _client.Execute(request_details);

                JToken vacancyDetails = JObject.Parse(response_details.Content);
                JArray keySkills = vacancyDetails["key_skills"] as JArray;
                if (keySkills.HasValues)
                {
                    foreach (JToken keySkill in keySkills)
                    {
                        listDetails.Add((string)keySkill["name"]);
                    }
                }
            }
            formWait.Hide();
            return listDetails;
        }

        /// <summary>
        /// Функция выводы результатов на форму
        /// </summary>
        /// <param name="data">Набор данных для вывода</param> 
        /// <param name="listBox">Окно (listbox) для вывода</param> 
        /// <param name="labelCount">Параметр числа найденных результатов</param> 
        /// <param name="labelCountClear">Число найденных различающихся результатов</param>
        private void ShowOnForm(List<string> data, ListBox listBox, Label labelCount, Label labelCountClear)
        {
            labelCount.Text = data.Count.ToString();
            data = data.Distinct().ToList();
            labelCountClear.Text = data.Count.ToString();
            listBox.DataSource = data;
        }

        /// <summary>
        /// Вызов функций при нажатии на кнопки:
        /// вызов функции получения списка вакансий,
        /// вызов функции получения названий вакансий или ключевых навыков
        /// и
        /// вызов функции отображения
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            listVacancyBigSalary = GetVacancy(true).ToList();
            List<string> data = GetVacanciesNames(listVacancyBigSalary);
            ShowOnForm(data, listBox1, labelCount1, labelCountClear1);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            List<string> data = GetVacanciesDetails(true).ToList();
            ShowOnForm(data, listBox2, labelCount2, labelCountClear2);
            if (listBox1.Items.Count == 0)
            {
                data = GetVacanciesNames(listVacancyBigSalary);
                ShowOnForm(data, listBox1, labelCount1, labelCountClear1);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            listVacancyLowSalary = GetVacancy(false).ToList();
            List<string> data = GetVacanciesNames(listVacancyLowSalary);
            ShowOnForm(data, listBox3, labelCount3, labelCountClear3);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            List<string> data = GetVacanciesDetails(false).ToList();
            ShowOnForm(data, listBox4, labelCount4, labelCountClear4);
            if (listBox3.Items.Count == 0)
            {
                data = GetVacanciesNames(listVacancyLowSalary);
                ShowOnForm(data, listBox3, labelCount3, labelCountClear3);
            }
        }
    }

}

