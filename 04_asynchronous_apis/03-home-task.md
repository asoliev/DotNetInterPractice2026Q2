Create Async REST APIs with ASP.NET Core. Use DAL designed in prev module. The following list of the resources should be available

1.  Venues
a.  GET /venues
b.  GET /venues/{venue_id}/sections returns all sections for venue

2.  Events
a.  GET /events
b.  GET /events/{event_id}/sections/{section_id}/seats list of seats (section_id, row_id, seat_id) with seats’ status (id, name) and price options (id, name)

3.  Orders
a.  GET orders/carts /{cart_id} gets list of items in a cart (cart_id is a uuid, generated and stored the client side)
b.  POST orders/carts/{cart_id} takes object of event_id, seat_id and price_id as a payload and adds a seat to the cart. Returns a cart state (with total amount) back to the caller)
c.  DELETE orders/carts/{cart_id}/events/{event_id}/seats/{seat_id} deletes a seat for a specific cart
d.  PUT orders/carts/{cart_id}/book moves all the seats in the cart to a booked state. Returns a PaymentId. Note1: You do not need to work out the validation of the seat status – we will do this in a later module.

4.  Payments
a.  GET payments/{payment_id} Returns the status of a payment
b.  POST payments/{payment_id}/complete Updates payment status and moves all the seats related to a payment to the sold state.
c.  POST payments/{payment_id}/failed Updates payment status and moves all the seats related to a payment to the available state.

Score board:

0-69% – The REST API was developed. All resources have their own controllers. Each request is handled by a separate action within a corresponding controller. The code is well formatted.
70-89% ­­– Written answers on the self-check questions of the module were provided.
90-100% ­– Written answers on the self-check questions of the ‘Async Programming’ and ‘REST Architecture’ were provided.