using System;
using System.Xml.Serialization;
using System.Web.Services.Protocols;
using System.Web.Services;

namespace WebServiceTest
{
    [System.Web.Services.WebServiceBindingAttribute(Name="TestServiceSoap", Namespace="http://tempuri.org/")]
    public class TestService : SoapHttpClientProtocol 
	{
        public UserInfo UserInfoValue;
        
        public TestService() {
            this.Url = "http://192.168.200.3:8080/TestService.asmx";
        }
        
		[SoapDocumentMethodAttribute("http://tempuri.org/Echo", RequestNamespace="http://tempuri.org/", ResponseNamespace="http://tempuri.org/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=SoapParameterStyle.Wrapped)]
		public string Echo(string a) 
		{
			object[] results = this.Invoke("Echo", new object[] {a});
			return ((string)(results[0]));
		}

        public System.IAsyncResult BeginEcho(string a, System.AsyncCallback callback, object asyncState) {
            return this.BeginInvoke("Echo", new object[] {a}, callback, asyncState);
        }
        
        public string EndEcho(System.IAsyncResult asyncResult) {
            object[] results = this.EndInvoke(asyncResult);
            return ((string)(results[0]));
        }
        
		[SoapDocumentMethodAttribute("http://tempuri.org/Add", RequestNamespace="http://tempuri.org/", ResponseNamespace="http://tempuri.org/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=SoapParameterStyle.Wrapped)]
        public int Add(int a, int b) {
            object[] results = this.Invoke("Add", new object[] { a, b});
            return ((int)(results[0]));
        }
        
        public System.IAsyncResult BeginAdd(int a, int b, System.AsyncCallback callback, object asyncState) {
            return this.BeginInvoke("Add", new object[] { a, b}, callback, asyncState);
        }
        
        public int EndAdd(System.IAsyncResult asyncResult) {
            object[] results = this.EndInvoke(asyncResult);
            return ((int)(results[0]));
        }
    }

	[Encrypt]
	[System.Web.Services.WebServiceBindingAttribute(Name="SimpleServiceSoap", Namespace="http://tempuri.org/")]
	public class ConverterService : SoapHttpClientProtocol 
	{
		public UserInfo UserInfoValue;
        
		public ConverterService() 
		{
			this.Url = "http://192.168.200.3:8080/ConverterService.asmx";
		}
        
		[SoapHeaderAttribute("UserInfoValue", Required=false, Direction=SoapHeaderDirection.Out)]
		[SoapDocumentMethodAttribute("http://tempuri.org/Login", RequestNamespace="http://tempuri.org/", ResponseNamespace="http://tempuri.org/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=SoapParameterStyle.Wrapped)]
		public void Login(string a) 
		{
			this.Invoke("Login", new object[] {a});
		}

		[SoapHeaderAttribute("UserInfoValue", Required=false)]
		[SoapDocumentMethodAttribute("http://tempuri.org/Convert", RequestNamespace="http://tempuri.org/", ResponseNamespace="http://tempuri.org/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=SoapParameterStyle.Wrapped)]
		public double Convert(string sourceCurrency, string targetCurrency, double value) 
		{
			object[] results = this.Invoke("Convert", new object[] {sourceCurrency, targetCurrency, value});
			return ((double)(results[0]));
		}

		[SoapHeaderAttribute("UserInfoValue", Required=false)]
		[SoapDocumentMethodAttribute("http://tempuri.org/GetCurrencyInfo", RequestNamespace="http://tempuri.org/", ResponseNamespace="http://tempuri.org/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=SoapParameterStyle.Wrapped)]
		public CurrencyInfo[] GetCurrencyInfo () 
		{
			object[] results = this.Invoke("GetCurrencyInfo", new object[0]);
			return ((CurrencyInfo[])(results[0]));
		}

		[SoapHeaderAttribute("UserInfoValue", Required=false)]
		[SoapDocumentMethodAttribute("http://tempuri.org/SetCurrencyRate", RequestNamespace="http://tempuri.org/", ResponseNamespace="http://tempuri.org/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=SoapParameterStyle.Wrapped)]
		public void SetCurrencyRate(string currency, double rate) 
		{
			this.Invoke("SetCurrencyRate", new object[] {currency, rate});
		}

		[SoapHeaderAttribute("UserInfoValue", Required=false)]
		[SoapDocumentMethodAttribute("http://tempuri.org/GetCurrencyRate", RequestNamespace="http://tempuri.org/", ResponseNamespace="http://tempuri.org/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=SoapParameterStyle.Wrapped)]
		public double GetCurrencyRate(string cname) 
		{
			object[] results = this.Invoke("GetCurrencyRate", new object[] {cname});
			return ((double)(results[0]));
		}
	}

	[XmlTypeAttribute(Namespace="http://tempuri.org/")]
	public class CurrencyInfo
	{
		public string Name;
		public double Rate;
	}
    
    [XmlTypeAttribute(Namespace="http://tempuri.org/")]
    [XmlRootAttribute(Namespace="http://tempuri.org/", IsNullable=false)]
    public class UserInfo : SoapHeader {
        
        public int userId;
    }
}
