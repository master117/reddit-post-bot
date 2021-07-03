# reddit-post-bot
A bot that uploads an image in set intervalls and posts it to a chosen reddit. Can be used for daily image posts which many reddits feature.


## Setup
reddit-post-bot requires a variety of api keys, deployed as txt files next to the exe:
* user.txt should contain the posting reddit users username.
* pw.txt should contain the posting reddit users password.
* targetreddit.txt should contain the reddit to be posted to. Prefixed with "/r/". Like: /r/pics

* clientid.txt should contain the posting reddit users api client id. Which can be aquired by creating a reddit application.
* secret.txt should contain the posting reddit users api secret. Which can be aquired by creating a reddit application.

* imgurclientid.txt should contain the posting imgur users api client id. Which can be aquired by creating a imgur application.
* imgursecret.txt should contain the posting imgur users api secret. Which can be aquired by creating a imgur application.

* imgurrefreshtoken.txt contains a refresh token aquired after first run of the application. It can remain empty until then.

## Usage
Deploy all images to be posted into a folder named "posts" next to the exe. 
By default one will be posted every 24hrs. You can adjust this timeout by updating the matching value in program.cs.
