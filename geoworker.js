self.onmessage = function(data) {



    var data = JSON.parse(data.data);
    var csv = data.csv;
    var res = data.res;
    csv = csv.filter(x => x.county != "Unknown" && x.state != "Virgin Islands" && x.state != "Guam" && x.county != "Saipan");
    //csv = csv.filter(x => x.state == "Maryland");
    csv = csv.forEach((x, idx) => {
        var loc = res.filter(y => y.COUNTY == x.county && y.STATE_NAME == x.state);
        if (loc.length == 0) {
            loc = res.filter(y => y.STATE_NAME == x.state);
        }
        try {
            x.lat = loc[0].LATITUDE;
            x.lng = loc[0].LONGITUDE;
        } catch (error) {
            console.log(x);
        }
        if(idx % 10000 == 0) {
            console.log(idx);
        }
        postMessage(x);
    });


}