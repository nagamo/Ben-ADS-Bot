This Bot is intended to provide Lead Qualification for automotive dealers that are doikng digital advertising on Facebook Messenger. The primary goals are:

1. Provide an entertaining, freewheeling and witty conversation.
2. Be able to provide answers to a wide-ranging array of car-purchasing questions - everything from handling poor credit to getting the most for their trade-in.
3. Pull sufficient data from them to consider them "qualified".

Definition of Qualified:

Use Case 1: No need for financing and no trade-in
            Minimum data:
			* Name and either phone number or email (phone number preferred)
			* A stated car (or type of car) of interest. In other words, they need to assert their intention to buy something
			* A stated timeframe in the near future for taking action. In other words, they need to assert their intention to buy (or at least look) within a week or two


Use Case 2: Needs financing, but not trading in a vehicle
			Minimum data:
			* Same as Use Case 1, PLUS:
										** Credit Score (just what they state it to be)
										** If credit score < 700, then also:
											*** Monthly income
											*** Status of home ownership

Use Case 3: Needs financing and is planning on trading in a vehicle
			Minimum data:
			* Same as Use Case 1 and 2, PLUS:
										** Make, Model and Year of vehicle
										** Mileage
										** Condition
										** Amount Owed

Architecture:
	1. Load an Activity Handler (or perhaps a dialog) and state a welcome message, along with three selections:
		* Identify a car (inventory)
		* Pursue financing
		* Value a trade-in
	2. ALWAYS allow free text
	3. Whether they makle a selection or type something, respond with "Hey, happy to do that but can I get your name and phone number in case we get disconnected"
	4. All utterances get passed through LUIS. If it gets reduced to a probablity greater than .75 that the utterance is one of those three intents,
	   or they select one of the choices, then after they're presented with prompts for name and phone, commence the appropriate dialog.
	5. Every utterance gets checked for cancellation - any of 'quit','cancel','stop', 'bye', etc. If they're in a dialog, drop out to the next object on the stack.
	6. Track which dialogs and which data items have been fulfilled. If they utter a dialog intent, check whether they've been there before, then check to see if 
	   they still need to fill in required data. If there is still information outstanding, enter the appropriate place in the dialog. If all the data has been provided,
	   pass the utterance on to QnA.
	7. If the utterance doesn't map to an intent, pass it on to QnA.
	8. The incoming channel is Facebook Messenger. I do not have knowledge yet of the data payload being provided. May include name and car they've clicked on.
	9. All users get passed to Zoho CRM via API, qualified or not.
	10. Qualified users get passed to a specific dealer's CRM via API. This will need to be picked up from a configuration file.
