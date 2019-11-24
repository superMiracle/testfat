//+------------------------------------------------------------------+
//|                                                   FAT_MT4Api.mq4 |
//|                        Copyright 2019, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property strict

#define MAGIC  20190628
extern string ex_sServerName = "FAT_GP";

#import "EASide.dll"
   bool startServer(string sServerName);
   bool stopServer();
   
   void receiveReq(string& sOP);
   void receive_getOrderList_symbol(string& sSymbol);
   void receive_getPositionList_symbol(string& sSymbol );
   void receive_getRate_symbol(string& sSymbol);
   int receive_reqDelorder_ticket();
   void receive_reqNewOrder_params(string& sSym, string& sCmd, string& sLots, string& sPrice);
   void receive_reqOrderClose_params(int& ticket, double& dLots, double& dPrice);
   
   void send_getAccountInfo(double dBalance, double dEquity, double dMargin);
   void send_getOrderList(int nTicket, string sSymbol, int nCmd, double dLots, double dPrice);
   void send_getOrderList_end();
   void send_getPositionList(int nTicket, string sSymbol, int nCmd, double dLots, double dOpenPrice, double dClosePrice, double dCommission, double dProfit);
   void send_getPositionList_end();
   void send_getRate(string sSymbol, double dAsk, double dBid);
   void send_reqDelOrder(bool bRet);
   void send_reqNewOrder(int nTicket, double dPrice);
   void send_reqOrderClose(bool bRet, double dClosePrice);
         
#import

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- create timer
   EventSetMillisecondTimer(1);
   startServer(ex_sServerName);
      
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- destroy timer
   EventKillTimer();
   stopServer();
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   
  }
//+------------------------------------------------------------------+
//| Timer function                                                   |
//+------------------------------------------------------------------+
void OnTimer()
{
      string sOP = "";
      receiveReq(sOP);
      //printf("receive : ", sOP);
      if ( sOP == "GET_RATE" )
         process_getRates();
         
      if ( sOP == "GET_ORDERS" )   
         process_getOrders();
         
      if ( sOP == "GET_POSITIONS" )
         process_getPositions();
      
      if ( sOP == "GET_ACCOUNT" )
         process_getAccInfo();
      
      if ( sOP == "REQ_NEWORDER")
         process_reqNewOrder();
      
      if ( sOP == "REQ_DELORDER" )
         process_reqDelOrder();
      
      if ( sOP == "REQ_ORDERCLOSE" )
         process_reqOrderClose();               
   
}

void process_getRates()
{
   string sSymbol = "";
   receive_getRate_symbol(sSymbol);
   //printf("receive_getRate_symbol : ", sSymbol);
   double dAsk = MarketInfo(sSymbol, MODE_ASK);
   double dBid = MarketInfo(sSymbol, MODE_BID);
   //Print("Ask=", dAsk, "Bid=", dBid);
   send_getRate(sSymbol, dAsk, dBid);
}

void process_getOrders()
{
   string sSymbol = "";
   receive_getOrderList_symbol(sSymbol);
   
   for ( int i = 0; i < OrdersTotal(); i ++ )
   {
      if ( !OrderSelect(i, SELECT_BY_POS ) ) continue;
      if ( OrderMagicNumber() != MAGIC ) continue;
      if ( StringCompare(OrderSymbol() , sSymbol, false)!= 0 ) continue;
      if ( OrderType() == OP_BUY || OrderType() == OP_SELL ) continue;
      
      send_getOrderList(OrderTicket(), sSymbol, OrderType(), OrderLots(), OrderOpenPrice());
   }
   send_getOrderList_end();
}

void process_getPositions()
{
   //string sSymbol = "";
   //receive_getPositionList_symbol(sSymbol);
   
   for ( int i = 0; i < OrdersTotal(); i ++ )
   {
      if ( !OrderSelect(i, SELECT_BY_POS) ) continue;
      
      if ( OrderMagicNumber() != MAGIC ) continue;
      
      //if ( StringCompare(OrderSymbol(), sSymbol, false) != 0 ) continue;
      
      if ( OrderType() != OP_BUY && OrderType() != OP_SELL ) continue;
      
      
      send_getPositionList(OrderTicket(), OrderSymbol(), OrderType(), OrderLots(), OrderOpenPrice(),
         OrderClosePrice(), OrderCommission(), OrderProfit());
   }
   send_getPositionList_end();
}

void process_getAccInfo()
{
   double dBalance = AccountBalance();
   double dEquity = AccountEquity();
   double dMargin = AccountMargin();
   
   send_getAccountInfo(dBalance, dEquity, dMargin);
}

void process_reqNewOrder()
{
   string sSymbol = "";
   string sCmd= "";
   string sLots= "";
   string sPrice= "";
   double dLots;
   double dPrice;
   receive_reqNewOrder_params(sSymbol, sCmd, sLots, sPrice);
   Print("receive_reqNewOrder_params", sSymbol, sCmd, sLots, sPrice);
   dLots = StrToDouble(sLots);
   dPrice = StrToDouble(sPrice);
   int nCmd = 0;
   
   if ( sCmd == "BUY" ) nCmd = OP_BUYSTOP;
   if ( sCmd == "SELL" ) nCmd = OP_SELLSTOP;
   if ( sCmd == "BUY_LIMIT" ) nCmd = OP_BUYSTOP;
   if ( sCmd == "SELL_LIMIT" ) nCmd = OP_SELLSTOP;
   
   int nDigits = MarketInfo(sSymbol, MODE_DIGITS);
   double dPoint = MarketInfo(sSymbol, MODE_POINT);
   
   double dCurPrice = 0;
   if ( nCmd == OP_BUYSTOP )
   {
      dCurPrice = MarketInfo(sSymbol, MODE_ASK);
      if ( dCurPrice > dPrice + dPoint * 2 )
      {
         Print("Price 2 pips over Moved!");
         send_reqNewOrder(-1, 0);
         return;
      }
   }
   else if ( nCmd == OP_SELLSTOP )
   {
      dCurPrice = MarketInfo(sSymbol, MODE_BID);
      if ( dCurPrice < dPrice - dPoint * 2 )
      {
         Print("Price 2 pips over Moved!");
         send_reqNewOrder(-1, 0);
         return;
      }
      
   }   
   
   int nTicket;
   //Print("OrderSend ", sSymbol, ",", nCmd, ",", dLots, ",", dCurPrice, ",");
   /*
   //For First Site
   nTicket = OrderSend(sSymbol, nCmd, NormalizeDouble(dLots,1), NormalizeDouble(dCurPrice, nDigits), 0, 0, 0, NULL, MAGIC);
   if ( nTicket < 0 )
   {
      Print("GetLast Error : ", GetLastError());
   }*/
   //For second site
   for ( int i = 0; i < 3; i ++ )
   {
      nTicket = OrderSend(sSymbol, nCmd, NormalizeDouble(dLots,1), NormalizeDouble(dCurPrice, nDigits), 0, 0, 0, NULL, MAGIC);
      if ( nTicket > 0 ) 
         break;
      if ( nCmd == OP_BUYSTOP )
      {
         dCurPrice = MarketInfo(sSymbol, MODE_ASK);
      }
      else if ( nCmd == OP_SELLSTOP )
      {
         dCurPrice = MarketInfo(sSymbol, MODE_BID);
      }   
   }
   
   if ( nTicket < 0 )
   {
      if ( nCmd == OP_BUYSTOP ) nCmd = OP_BUY;
      if ( nCmd == OP_SELLSTOP ) nCmd = OP_SELL;
      nTicket = OrderSend(sSymbol, nCmd, NormalizeDouble(dLots,1), NormalizeDouble(dCurPrice, nDigits), 0, 0, 0, NULL, MAGIC);
   }
   
   if ( nTicket > 0 ) 
   {
      if ( OrderSelect(nTicket, SELECT_BY_TICKET) )
         dPrice = OrderOpenPrice();
   }
   send_reqNewOrder(nTicket, dPrice);
}

void process_reqDelOrder()
{
   int nTicket = receive_reqDelorder_ticket();
   bool bRet = OrderDelete(nTicket);
   send_reqDelOrder(bRet);
}

void process_reqOrderClose()
{
   int nTicket = 0;
   double dLots = 0;
   double dClosePrice = 0;
   receive_reqOrderClose_params(nTicket, dLots, dClosePrice);
   Print("OrderClose : ticket=", nTicket, "lots=",dLots, "price=",dClosePrice);
   int nDigits = MarketInfo(OrderSymbol(), MODE_DIGITS);
   bool bRet = OrderClose(nTicket, NormalizeDouble(dLots,1), NormalizeDouble(dClosePrice, nDigits), 5);
   if (bRet )
   {
      if ( OrderSelect(nTicket, SELECT_BY_TICKET, MODE_HISTORY) )
         dClosePrice = OrderClosePrice();
   }
   send_reqOrderClose(bRet, dClosePrice);
}
//+------------------------------------------------------------------+
