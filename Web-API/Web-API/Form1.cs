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
        /// Функция возвращает коэффициент для перевода Salary в RUB
        /// </summary>
        /// <param name="vacancy"></param>
        /// <returns></returns>
        private double TranslateRate(JToken currency)
        {
            foreach (JToken _curr in curr)
            {
                if((string)currency == (string)_curr["code"])
                {
                    return (double)_curr["rate"];           
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
        /// <param name="flagBigSalary"></param> Флаг для поиска 120000
        /// <param name="flagDetails"></param> Флаг для поиска ключевых навыков
        /// <returns></returns>
        private List<string> GetInformation(bool flagBigSalary, bool flagDetails)
        {
            Form formWait = new FormWait();
            formWait.StartPosition = FormStartPosition.CenterParent;
            formWait.Show(this);
            IRestRequest request;
            int pagesCount = 0;
            int FirstPage = 0;
            List<string> listProf = new List<string>();
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

                    if (!flagDetails) 
                    {
                        // Если интересуют профессии
                        if (flagBigSalary && salary >= bigSalary)
                            listProf.Add((string)vacancy["name"]);
                        else if (!flagBigSalary && salary < lowSalary && salary > 0)
                            listProf.Add((string)vacancy["name"]);
                    }
                    else // Если интересуют ключевые навыки
                    {
                        if ((flagBigSalary && salary >= bigSalary) || (!flagBigSalary && salary < lowSalary && salary > 0))
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
            formWait.Hide();
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
            List<string> data = GetInformation(true, false).ToList();
            ShowOnForm(data, listBox1, labelCount1, labelCountClear1);          
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            List<string> data = GetInformation(true, true).ToList();
            ShowOnForm(data, listBox2, labelCount2, labelCountClear2);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            List<string> data = GetInformation(false,false).ToList();
            ShowOnForm(data, listBox3, labelCount3, labelCountClear3);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            List<string> data = GetInformation(false, true).ToList();
            ShowOnForm(data, listBox4, labelCount4, labelCountClear4);          
        }
    }

}

