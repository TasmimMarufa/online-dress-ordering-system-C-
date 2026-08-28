using dress_ordering_system.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dress_ordering_system.Controllers
{
    public class CustomerController : Controller
    {
        private myContext _context;
        private IWebHostEnvironment _env;

        public CustomerController(myContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public IActionResult Index()
        {
            List<Category> category = _context.tbl_category.ToList();
            ViewData["category"] = category;
            List<Dress> dresses = _context.tbl_dress.ToList();
            ViewData["dress"] =dresses;
            ViewBag.checkSession = HttpContext.Session.GetString("customerSession");
            return View();
        }
        public IActionResult Login()
        {
           
            return View();
        }
        [HttpPost]
        public IActionResult Login(string customerEmail,string customerPassword)
        {
          var customer=  _context.tbl_customer.FirstOrDefault(c => c.customer_email == customerEmail);
            if (customer!=null && customer.customer_password== customerPassword)
            {
                HttpContext.Session.SetString("customerSession", customer.customer_id.ToString());
                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.Error = "Invalid email or password";
                return View();
               
            }
               
        }
        public IActionResult CustomerRegistration()
        {

            return View();
        }
        [HttpPost]
        public IActionResult CustomerRegistration(Customer customer)
        {
            _context.tbl_customer.Add(customer);
            _context.SaveChanges();
            return RedirectToAction("Login");
        }
        public IActionResult customerLogout()
        {
            HttpContext.Session.Remove("customerSession");
            return RedirectToAction("index");
        }
        public IActionResult customerProfile()
        {
            if(string.IsNullOrEmpty(HttpContext.Session.GetString("customerSession")))
            {
                return RedirectToAction("Login");
            }
            else
            {
                List<Category> category = _context.tbl_category.ToList();
                ViewData["category"] = category;
                var customerId = HttpContext.Session.GetString("customerSession");
                var row = _context.tbl_customer
                    .Where(c => c.customer_id == int.Parse(customerId))
                    .ToList();

                return View(row);
               
            }
               
        }
        [HttpPost]
        public IActionResult updateCustomerProfile(Customer customer)
        {
            _context.tbl_customer.Update(customer);
            _context.SaveChanges();
            return RedirectToAction("customerProfile");
        }
        public IActionResult changeProfileImage(Customer customer, IFormFile customer_image)
        {
            if (customer_image == null || customer_image.Length == 0)
            {
                return RedirectToAction("customerProfile");
            }

            string ImagePath = Path.Combine(_env.WebRootPath, "customer_image", customer_image.FileName);

            FileStream fs = new FileStream(ImagePath, FileMode.Create);
            customer_image.CopyTo(fs);
            fs.Close();

            customer.customer_image = customer_image.FileName;

            _context.tbl_customer.Update(customer);
            _context.SaveChanges();

            return RedirectToAction("customerProfile");
        }
        public IActionResult feedback()
        {
            List<Category> category = _context.tbl_category.ToList();
            ViewData["category"] = category;
            return View();
        }

        [HttpPost]
        public IActionResult feedback(Feedback feedback)
        {
            TempData["message"] = "Feedback Successfully Submitted";
            _context.tbl_feedback.Add(feedback);
            _context.SaveChanges();
            return RedirectToAction("feedback");
        }
        public IActionResult fetchAllDress()
        {
            List<Category> category = _context.tbl_category.ToList();
            ViewData["category"] = category;

            List<Dress> dresses = _context.tbl_dress.ToList();

            return View(dresses);   // 👈 send dresses as the MODEL
        }
        public IActionResult DressDetails(int id)
        {
            List<Category> category = _context.tbl_category.ToList();
            ViewData["category"] = category;
         var dress =  _context.tbl_dress.Where(p => p.dress_id == id).ToList();
            return View(dress);
        }
        public IActionResult AddToCart(int dress_id, Cart cart)
        {
            string isLogin = HttpContext.Session.GetString("customerSession");

            // 🔒 NOT LOGGED IN → Go to Login
            if (string.IsNullOrEmpty(isLogin))
            {
                return RedirectToAction("Login");
            }

            // 🛒 LOGGED IN → Add product to cart
            cart.dress_id = dress_id;
            cart.cust_id = int.Parse(isLogin);
            cart.dress_quantity = 1;
            cart.cart_status = 0;

            _context.tbl_cart.Add(cart);
            _context.SaveChanges();

            TempData["message"] = "Product Successfully Added in Cart";

            return RedirectToAction("fetchAllDress");
        }
        public IActionResult fetchCart()
        {
            List<Category> category = _context.tbl_category.ToList();
            ViewData["category"] = category;

            string customerId = HttpContext.Session.GetString("customerSession");

            if (customerId != null)
            {
                var cart = _context.tbl_cart
                                   .Where(c => c.cust_id == int.Parse(customerId))
                                   .Include(c => c.products)
                                   .ToList();

                return View(cart);
            }
            else
            {
                return RedirectToAction("Login");
            }
        }
        public IActionResult removeProduct(int id)
        {
            var product = _context.tbl_cart.Find(id);
            _context.tbl_cart.Remove(product);
            _context.SaveChanges();

            return RedirectToAction("fetchCart");
        }
        public IActionResult checkoutProduct()
        {
            string customerId = HttpContext.Session.GetString("customerSession");
            if (customerId == null)
                return RedirectToAction("customerLogin");

            ViewData["category"] = _context.tbl_category.ToList();

            int id = int.Parse(customerId);

            var cart = _context.tbl_cart
                               .Where(c => c.cust_id == id)
                               .Include(c => c.products)
                               .ToList();

            return View(cart);
        }
        public IActionResult PlaceOrder()
        {
            string customerId = HttpContext.Session.GetString("customerSession");

            if (customerId == null)
                return RedirectToAction("Login");

            var cartItems = _context.tbl_cart
                                    .Where(c => c.cust_id == int.Parse(customerId))
                                    .Include(c => c.products)
                                    .ToList();

            if (!cartItems.Any())
                return RedirectToAction("fetchCart");

            decimal totalAmount = 0;

            foreach (var item in cartItems)
            {
                decimal price = Convert.ToDecimal(item.products.dress_price.Replace(" tk", ""));
                totalAmount += item.dress_quantity * price;
            }

            Order order = new Order()
            {
                cust_id = int.Parse(customerId ?? "0"),
                order_date = DateTime.Now,
                total_amount = totalAmount,
                status = "Placed"
            };

            _context.tbl_order.Add(order);
            _context.SaveChanges();

            _context.tbl_cart.RemoveRange(cartItems);
            _context.SaveChanges();

            return RedirectToAction("OrderSuccess");
        }


        public IActionResult OrderSuccess()
        {
            var category = _context.tbl_category.ToList();
            ViewData["category"] = category;

            return View();
        }

        public IActionResult MyOrders()
        {
            string customerId = HttpContext.Session.GetString("customerSession");

            if (customerId == null)
                return RedirectToAction("Login");

            int cid = int.Parse(customerId);

            var orders = _context.tbl_order
                                 .Where(o => o.cust_id == cid)
                                 .OrderByDescending(o => o.order_date)
                                 .ToList();

            ViewData["category"] = _context.tbl_category.ToList();

            return View(orders);
        }

        public IActionResult Blog()
        {
            var blogs = _context.tbl_blog
                                .OrderByDescending(b => b.created_date)
                                .ToList();

            ViewData["category"] = _context.tbl_category.ToList();
            return View(blogs);
        }

        public IActionResult BlogDetails(int id)
        {
            var blog = _context.tbl_blog.FirstOrDefault(b => b.blog_id == id);

            ViewData["category"] = _context.tbl_category.ToList();
            return View(blog);
        }


    }
}
