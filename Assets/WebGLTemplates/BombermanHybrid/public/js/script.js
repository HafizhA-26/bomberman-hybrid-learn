var container = document.querySelector("#unity-container");
var canvas = document.querySelector("#unity-canvas");
var loadingGroup = document.querySelector("#bomberman-loading");
var progressBarFill = document.querySelector("#loading-fill");
var progressPercentage = document.querySelector("#loading-percentage");
var warningBanner = document.querySelector("#unity-warning");
var progressBarText = 0;
var maxScreenHeight = 0;
progressBarFill.style.width = "0%";

document.addEventListener("focusin", showInputBox);
document.addEventListener("focusout", hideInputBox);

function loadGame(config)
{
    canvas = document.querySelector("#unity-canvas");
    
    container.style.width = "0%";
    container.style.height = "0%"

    createUnityInstance(canvas, config, (progress) => {
        progressBarText = 100 * progress + "%"
        progressBarFill.style.width = progressBarText;

        progressPercentage.textContent = Number(100 * progress).toFixed(0)+ "%";
      }).then((unityInstance) => {
        window.unityInstance = unityInstance;
        loadingGroup.style.display = "none";
        container.style.width = "100%";
        container.style.height = "100%"
      }).catch((message) => {
        alert(message);
    });
}

function showInputBox(event)
{
    setTimeout(function(){
        if (event.target.tagName === "INPUT" || event.target.tagName === "TEXTAREA") {
            event.target.style.position = "fixed";
            event.target.style.top = "30px";    
            event.target.style.bottom = "auto"; 
            event.target.style.left = "50%";   
            event.target.style.transform = "translateX(-50%)"; 
            event.target.style.zIndex = "9999";
            
            event.target.style.WebkitAppearance = "none";
            event.target.style.height = "40px"
            event.target.style.fontSize = "20px";  
            event.target.style.padding = "10px";   
            event.target.style.borderRadius = "12px"; 
            event.target.style.border = "2px solid #FFFFFF";
            event.target.style.backgroundColor = "#ebebebb0"; 
            event.target.style.color = "#000000"; 
            event.target.style.boxShadow = "0px 10px 30px rgba(0,0,0,0.5)";
            
            event.target.style.outline = "none";
            event.target.maxLength = 8;
            
            // Prevent iphone for scrolling when keyboard opened
            var scrollInterval = setTimeout(function() {
                window.scrollTo(0, 0);
                document.body.scrollTop = 0;
            }, 50);

            setTimeout(function() {
                clearInterval(scrollInterval);
            }, 500);
        }
    }, 50);
}

function hideInputBox(event)
{
    if (event.target.tagName === "INPUT" || event.target.tagName === "TEXTAREA") {
        event.target.style.position = "fixed";
        event.target.style.top = "-1000px";
        event.target.style.bottom = "auto";
    }
}

function showBannerMessage(msg, type)
{
    warningBanner.innerHTML = msg;
    if (type == 'error') warningBanner.style = "display: block; color: #BC4749;";
    else if(type == 'warning') warningBanner.style = "display: block; color: #b98e00;"

    setTimeout(() => {
        warningBanner.style.display = 'none';
    }, 10000);
}

