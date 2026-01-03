using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;
using Webhttp.Models;



namespace Webhttp.DBProviders
{
	public class MongoProvider
	{
		public IMongoClient _mongoClient;
		public IMongoDatabase _mongoDatabase;
	    public IMongoCollection<contactsPostMongo> contactsPostsMon { get; }

        public IMongoCollection<registerPostMongo> registerPostsMon { get; }
        public IMongoCollection<groupsPostMongo> groupsPostPostsMon { get; }
        public IMongoCollection<groupcontactsPostMongo> groupcontactsPostMon { get; }

        public IMongoCollection<AudioPostMongo> AudioPostMon { get; }

        public IMongoCollection<AudiogroupMongo> AudiogroupPostMon { get; }

        public MongoProvider(IConfiguration config, IMongoClient _client)
		{
			_mongoClient = _client;
			_mongoDatabase = _mongoClient.GetDatabase("contact_group");
            contactsPostsMon = _mongoDatabase.GetCollection<contactsPostMongo>("contacts");
            groupsPostPostsMon = _mongoDatabase.GetCollection<groupsPostMongo>("groups");
            groupcontactsPostMon = _mongoDatabase.GetCollection<groupcontactsPostMongo>("groupcontacts");
            AudioPostMon = _mongoDatabase.GetCollection<AudioPostMongo>("Audio");
            AudiogroupPostMon = _mongoDatabase.GetCollection<AudiogroupMongo>("Audiogroup");
            registerPostsMon = _mongoDatabase.GetCollection<registerPostMongo>("user");
        }

        


        public async Task<contactsPostMongo> isfilescontactExist(string id,string filename)
        {

            var filter = new BsonDocument();

            var Builder = Builders<contactsPostMongo>.Filter;
            var query = Builder.Where(r => r.filename == filename) &
                Builder.Where(r => r.uid == id);

            var filenameList = await contactsPostsMon.FindAsync(query);
            var reponse = filenameList.FirstOrDefault();
            return reponse;

            // var categoriesList = await contactsPostsMon.DistinctAsync<string>("filename", query);
        }


        public async Task bulkcontacts(List<createcontactsPostUid> post)
        {
			List<contactsPostMongo> lst = new List<contactsPostMongo>();


            foreach (var contact in post)
			{
                contactsPostMongo postM = new contactsPostMongo
                {
                    uid = contact.uid,
                    name = contact.name,
                    phone = contact.phone,
                    filename=contact.filename,

                };
                lst.Add(postM);
            }


			await contactsPostsMon.InsertManyAsync(lst);
        }


        public async Task bulkdeletegroupcontacts(createcontactsgroupPost post,string id)
        {



            var Builder = Builders<groupcontactsPostMongo>.Filter;
            var query = Builder.Where(r => r.Groupid == post.id) &
                               Builder.Where(r => r.uid == id);

            var personsDeleteResult = await groupcontactsPostMon.DeleteManyAsync(query);
           
         

        }

      

        public async Task<AudiogroupMongo> getAudiobyidPost(getAudiobyidReq post,string id)
        {
            var Builder = Builders<AudiogroupMongo>.Filter;

            var query = Builder.Where(r => r.groupid == post.groupid)
                & Builder.Where(r => r.uid == id); ;

            var myres = await AudiogroupPostMon.FindAsync(query);

           
           var reponse= myres.FirstOrDefault() ;
            return reponse;
        }
        public async Task<userPost> Login(loginPost post)
        {
            var Builder = Builders<registerPostMongo>.Filter;
            var query = Builder.Where(r => r.username == post.userName) &
                               Builder.Where(r => r.password  == post.password);


            var myres = await registerPostsMon.FindAsync(query);


            var response = myres.FirstOrDefault();

            if (response!= null)
            {
                userPost myuser = new userPost
                {
                    id = response.id.ToString(),
                    username = response.username

                };



                return myuser;
            }
            else
                return null;

            
        }
        public async Task bulkgroupcontacts(createcontactsgroupPost post,string id)
        {
            List<contactsPostMongo> lst  = await contactsPostsMon.Find(x => true).ToListAsync();

            List<groupcontactsPostMongo> lstgrpcontacts = new List<groupcontactsPostMongo>(); 

            foreach (var contact in lst)
            {
                groupcontactsPostMongo postM = new groupcontactsPostMongo
                {
                    uid = id,
                    Groupid = post.id,
                    name = contact.name,
                    phone = contact.phone,  

                };
                lstgrpcontacts.Add(postM);
            }


            await groupcontactsPostMon.InsertManyAsync(lstgrpcontacts);
        }




       




        public async Task createregisterPost(createregisterPost post)
        {
            registerPostMongo postM = new registerPostMongo
            {
                fullname = post.fullname ,
                email = post.email,
                password = post.password,
                username = post.username

            };

            await registerPostsMon.InsertOneAsync(postM);
        }

        public async Task CreatecontactsPost(createcontactsPost post)
		{
			contactsPostMongo postM = new contactsPostMongo
			{
				name = post.name,
				phone = post.phone

			};
			
			await contactsPostsMon.InsertOneAsync(postM);
		}
      
        public async Task CreategroupsPost(creategroupsPostui post)
        {
            groupsPostMongo postM = new groupsPostMongo
            {
                name = post.name,
                uid  =post.uid  
  
            };

            await groupsPostPostsMon.InsertOneAsync(postM);
        }

        public async Task createAudiogroup(createAudiogroupPost post,string id)
        {

            //var query = Builders<AudiogroupMongo>.Filter.Where(r => r.groupid.ToString() == post.groupid);
          
            var filter = Builders<AudiogroupMongo>.Filter.Where(r => r.groupid.ToString() == post.groupid);
            filter = filter & Builders<AudiogroupMongo>.Filter.Where(r => r.uid.ToString() == id);



            var DeleteResult = await AudiogroupPostMon.DeleteOneAsync(filter);

            AudiogroupMongo postM = new AudiogroupMongo
            {
                uid =id,
                groupid = post.groupid,
                audioid = post.audioid

            };

            await AudiogroupPostMon.InsertOneAsync(postM);
        }

        public async Task deleteAudiorecordPost(deleteAudioPost post)
        {
            
            try
            {
                var query = Builders<AudioPostMongo>.Filter.Where(r => r.id.ToString()== post.id);

                var DeleteResult = await AudioPostMon.DeleteOneAsync(query);
            }
            catch (Exception ex)
            {

                string msg = ex.Message;
            }             

          

        }
        public async Task deletegroupPost(deletegroup post)
        {

            try
            {
                var query = Builders<groupsPostMongo>.Filter.Where(r => r.id.ToString() == post.id);

                var DeleteResult = await groupsPostPostsMon.DeleteOneAsync(query);
            }
            catch (Exception ex)
            {

                string msg = ex.Message;
            }
        }

        public async Task CreateAudio(createAudio post)
        {
            AudioPostMongo postM = new AudioPostMongo
            {
                uid = post.uid,
                name = post.name,
                filename = post.filename

            };
            await AudioPostMon.InsertOneAsync(postM);
        }

        public async Task<IEnumerable<AudioPost>> GetAudio(string id)
        {
            var query = Builders<AudioPostMongo>.Filter.Where(r => r.uid.ToString() == id);

            var myres = await AudioPostMon.FindAsync(query);

            var res = myres.ToList();

           

            List<AudioPost> mylist = new List<AudioPost>();
            foreach (var item in res)
            {
                AudioPost myAudio = new AudioPost
                {
                    id = item.id.ToString(),
                    name = item.name,
                    filename = item.filename

                };

                mylist.Add(myAudio);
            }
            return mylist;

        }

        public async Task<IEnumerable<contactsPost>> GetcontactsPost(string id)
		{
            var query = Builders<contactsPostMongo>.Filter.Where(r => r.uid.ToString() == id);

            var myres = await contactsPostsMon.FindAsync(query);

            var res = myres.ToList();

            List<contactsPost> mylist = new List<contactsPost>();
            foreach (var item in res)
            {
				contactsPost mycontacts = new contactsPost
				{
					id = item.id.ToString(),
                    uid=item.uid,
					name = item.name,
					phone = item.phone

				};

                mylist.Add(mycontacts);
            }
            return mylist;

        }

        public async Task<IEnumerable<groupcontactsPost>> getgroupcontacts(createcontactsgroupPost post,string id)
        {

           List<groupcontactsPost> mylist = new List<groupcontactsPost>();
            try
            {
                var filter = Builders<groupcontactsPostMongo>.Filter.Where(r => r.Groupid.ToString() == post.id);
                 filter = filter & Builders<groupcontactsPostMongo>.Filter.Where(r => r.uid.ToString() == id);

                var res = await groupcontactsPostMon.FindAsync(filter); ;

              

                var myres = await res.ToListAsync();

                foreach (var item in myres)
                {
                    groupcontactsPost mygroupcontacts = new groupcontactsPost
                    {
                        id = item.id.ToString(),
                        name = item.name,
                        phone = item.phone

                    };

                    mylist.Add(mygroupcontacts);
                }

            }
            catch (Exception ex)
            {

                string msg = ex.Message;
            }

            return mylist;
        }

        public async Task<IEnumerable<groupsPost>> getgroupsPost(string id)
        {
           
            var query = Builders<groupsPostMongo>.Filter.Where(r => r.uid.ToString() == id);

            var myres = await groupsPostPostsMon.FindAsync(query);

            var res = myres.ToList();


            List<groupsPost> mylist = new List<groupsPost>();
            foreach (var item in res)
            {
                groupsPost mygroup = new groupsPost
                {
                    id = item.id.ToString(),
                    name= item.name

                };

                mylist.Add(mygroup);
            }
            return mylist;

        }


    }
}
