# KubeCart — User Manual

Welcome to KubeCart, a microservice-based online store. This guide covers everything you need to know as a shopper or admin.

---

## Table of Contents

1. [Getting Started](#1-getting-started)
   - [Creating an Account](#11-creating-an-account)
   - [Logging In](#12-logging-in)
   - [Logging Out](#13-logging-out)
2. [Shopping](#2-shopping)
   - [Browsing Products](#21-browsing-products)
   - [Filtering by Category](#22-filtering-by-category)
   - [Viewing a Product](#23-viewing-a-product)
3. [Cart](#3-cart)
   - [Adding Items](#31-adding-items)
   - [Updating Quantities](#32-updating-quantities)
   - [Removing Items](#33-removing-items)
   - [Viewing Your Cart](#34-viewing-your-cart)
4. [Checkout](#4-checkout)
5. [Order History](#5-order-history)
6. [Admin Panel](#6-admin-panel)
   - [Managing Products](#61-managing-products)
   - [Managing Orders](#62-managing-orders)

---

## 1. Getting Started

### 1.1 Creating an Account

1. Open the app in your browser (e.g. `http://localhost:8080`).
2. Click **Register** in the top-right navigation bar.
3. Fill in your **First Name**, **Last Name**, **Email**, and **Password**.
4. Click **Create Account**.
5. You will be automatically logged in and redirected to the home page.

> **Password requirements:** At least 8 characters. A mix of letters, numbers, and symbols is recommended.

---

### 1.2 Logging In

1. Click **Login** in the navigation bar.
2. Enter your registered **Email** and **Password**.
3. Click **Sign In**.
4. On success, you are redirected to the home page and the navbar shows your first name.

> Your session is stored in the browser. You will remain logged in for 7 days, or until you explicitly log out.

---

### 1.3 Logging Out

Click your name in the top-right corner of the navigation bar, then click **Logout**. You will be redirected to the login page.

---

## 2. Shopping

### 2.1 Browsing Products

The **Home** page shows all available products. Each product card displays:

- Product image
- Product name
- Price
- Category
- An **Add to Cart** button

Scroll down to see more products.

---

### 2.2 Filtering by Category

On the Home page, a row of **category filter buttons** appears above the product grid. Click a category name to show only products in that category. Click it again (or click **All**) to clear the filter.

---

### 2.3 Viewing a Product

Click anywhere on a product card (except the Add to Cart button) to open the **Product Detail** page. Here you can read the full product description, see a larger image, and choose a quantity before adding to cart.

---

## 3. Cart

### 3.1 Adding Items

- **From the home page:** Click the **Add to Cart** button on any product card. The cart icon in the navbar updates its count.
- **From the product detail page:** Choose a quantity (default: 1) and click **Add to Cart**.

If the item is already in your cart, the quantity is incremented by the amount you added.

---

### 3.2 Updating Quantities

1. Click the **cart icon** in the navbar to open the **Cart Drawer**.
2. Use the **−** and **+** buttons next to an item to decrease or increase the quantity.
3. Changes are saved immediately.

---

### 3.3 Removing Items

In the Cart Drawer, click the **🗑 trash** icon to the right of any item to remove it entirely from your cart.

---

### 3.4 Viewing Your Cart

You can also click **Cart** in the navbar to go to the full **Cart Page**, which shows:

- All items with name, unit price, quantity, and line total
- A subtotal at the bottom
- A **Proceed to Checkout** button

Your cart is saved in the database. It persists across browser refreshes and login sessions.

---

## 4. Checkout

1. From the Cart Page or Cart Drawer, click **Proceed to Checkout**.
2. You must be logged in — if not, you will be redirected to the login page first.
3. On the **Checkout Page**, enter your **Shipping Address**.
4. Review the order summary (items and total).
5. Click **Place Order**.
6. On success, you will see a confirmation message with your **Order ID**.
7. Your cart is automatically cleared after a successful checkout.

> **Note:** Prices are locked at checkout time. Future price changes to products will not affect placed orders.

---

## 5. Order History

1. Click your name in the navbar, then click **My Orders**, or navigate directly to `/orders`.
2. The **Orders Page** shows a list of all your past orders, including:
   - Order ID
   - Date placed
   - Number of items
   - Total amount
   - **Status** (Pending / Processing / Shipped / Delivered / Cancelled)
3. Click on any order row to view its full details, including the list of items ordered and the shipping address.

---

## 6. Admin Panel

The Admin Panel is only accessible to users with the **admin** role. Admin menu items appear in the navbar when you are logged in as an admin.

> To create an admin user: after registering, update the `Role` column in the `KubeCart_Identity.Users` table to `admin`.

---

### 6.1 Managing Products

Navigate to **Admin → Products** (or `/admin/products`).

#### Add a New Product

1. Click **Add Product**.
2. Fill in:
   - **Name** — product display name
   - **Description** — full product description
   - **Price** — decimal value (e.g. `29.99`)
   - **Category** — select from the dropdown
   - **Stock** — number of units available
   - **Image URL** — a public URL to the product image
3. Click **Save**.

#### Edit a Product

1. Find the product in the table.
2. Click the **Edit** (pencil) icon on its row.
3. Update any fields.
4. Click **Save**.

#### Update a Product Image

1. Click the **Image** icon on the product row.
2. Enter the new image URL.
3. Click **Update Image**.

#### Delete a Product

1. Click the **Delete** (trash) icon on the product row.
2. Confirm the deletion in the dialog.

> **Warning:** Deleting a product does not remove it from existing orders. Order history is preserved.

---

### 6.2 Managing Orders

Navigate to **Admin → Orders** (or `/admin/orders`).

The admin orders page shows **all orders** from all customers, with:

- Order ID
- Customer name / email
- Date placed
- Item count
- Total amount
- Current status

#### Update an Order Status

1. Click the **Status** dropdown next to an order.
2. Select the new status:
   - `Pending` — order received, not yet processed
   - `Processing` — being picked and packed
   - `Shipped` — dispatched to carrier
   - `Delivered` — confirmed delivery
   - `Cancelled` — order cancelled
3. The status is updated immediately and the customer will see the change on their Orders page.
