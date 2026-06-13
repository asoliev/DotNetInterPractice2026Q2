Update logic of the ‘POST orders/carts/{cart_id}’ endpoint to throw an exception when booking occurs on a seat that is already booked or sold.
Perform 1000 requests in parallel for booking the same seat. Count successful responses. There should be more than 1. Try to use debugger or logging techniques to troubleshoot (investigate) this case.
Use debugging and logic techniques for troubleshooting the reasons why more than 1 successful response is returned.

Task 1
Consider reworking ‘‘POST orders/carts/{cart_id}’ endpoint to follow pessimistic concurrency strategy. Perform 1000 requests to ensure only 1 successful request is returned.

Task 2
Consider reworking ‘‘POST orders/carts/{cart_id}’ endpoint to follow optimistic concurrency strategy. Perform 1000 requests to ensure only 1 successful request is returned.

Score board:

0-69% –  Written answers on the self-check questions of the module were provided.
70-89% – Task 1 performed with no issues.
90-100% -  Task 2 was performed with no issues.