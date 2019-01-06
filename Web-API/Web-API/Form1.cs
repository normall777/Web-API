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

        public class listProf
        {
            public listProf()
            {           
                __list = new List<String>();         
            }
            public List<String> __list { get; private set; }
        }

        public Form1()
        {
            InitializeComponent();
        }




        private async void button1_Click(object sender, EventArgs e)
        {
            listProf _listi_ = new listProf();

            IRestRequest request = new RestRequest(string.Format("{0}?page={1}&per_page={2}&salary={3}&only_with_salary=true", HhApiVacanciesResource, VacanciesFirstPage, VacanciesPerPage, bigSalary), Method.GET);
            IRestResponse response = _client.Execute(request);    //RequestVacancies(VacanciesFirstPage);
            int pagesCount = (int)JObject.Parse(response.Content)["pages"];
            JArray vacancies = JObject.Parse(response.Content)["items"] as JArray;
            for (int i = VacanciesFirstPage; i < pagesCount; i++)
            {
                foreach (JToken vacancy in vacancies)
                {
                    _listi_.__list.Add((string)vacancy["name"]);

                }
                int FirstPage = VacanciesFirstPage + i;
                request = new RestRequest(string.Format("{0}?page={1}&per_page={2}&salary={3}&only_with_salary=true", HhApiVacanciesResource, FirstPage, VacanciesPerPage, bigSalary), Method.GET);
                response = _client.Execute(request);
                vacancies = JObject.Parse(response.Content)["items"] as JArray;
            }

            listBox1.DataSource = _listi_.__list;


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


    }

}

