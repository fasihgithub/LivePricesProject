---

# **LivePricesProject**

## **Running the LivePricesProject Locally (VS Code)**

### **1. Prerequisites**

Before you start, make sure your system has:

* .NET 7 SDK installed
* Visual Studio Code (VS Code) installed
* Internet access to fetch live price data from Binance

Verify your .NET installation by running in your terminal:
**dotnet --version**

---

### **2. Clone the Project**

Open a terminal and clone the GitHub repository:
**git clone [https://github.com/fasihgithub/LivePricesProject.git](https://github.com/fasihgithub/LivePricesProject.git)**
**cd LivePricesProject**

This will create a local copy of both the Web API service and the console client.

---

### **3. Restore Dependencies**

Restore all required NuGet packages:
**cd LivePricesService**
**dotnet restore**

Then:
**cd ../LivePriceClient**
**dotnet restore**

This ensures both projects have all the libraries they need.

---

### **4. Open Two Terminals**

You’ll need two terminals for running the project:

* **Terminal 1:** Run the Web API / WebSocket server (LivePricesService)
* **Terminal 2:** Run the console client (LivePriceClient)

---

### **5. Run the Web API / WebSocket Server**

In Terminal 1, navigate to the service folder and start the server:
**cd LivePricesService**
**dotnet run**

The server will start at:
**[http://localhost:5132](http://localhost:5132)**

You can open your browser and go to:
**[http://localhost:5132/swagger/index.html](http://localhost:5132/swagger/index.html)**

This shows all available REST API endpoints through Swagger.

Server logs will display:

* Client connections/disconnections
* Subscriptions/unsubscriptions
* Broadcasted price updates
* Total connected clients

---

### **6. Run the Console Client**

In Terminal 2, navigate to the client folder and start the console client:
**cd LivePriceClient**
**dotnet run**

The client connects to the WebSocket server automatically and subscribes to BTCUSD:
Connected to WebSocket server.
Subscribed to BTCUSD.

Live price updates will appear in real time, for example:
Received: {"symbol":"BTCUSD","price":105084.05,"timestamp":"2025-11-11T10:22:05Z"}
Received: {"symbol":"BTCUSD","price":105090.12,"timestamp":"2025-11-11T10:22:06Z"}

---

### **7. Stopping the Services**

Press **Ctrl + C** in Terminal 1 to stop the server.

---

