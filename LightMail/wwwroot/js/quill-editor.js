window.quillEditor = {

    editors: {},


    init: function(id, content) {

        var editor = new Quill(
            "#" + id,
            {
                theme: "snow",
                modules: {
                    toolbar:[
                        [
                            'bold',
                            'italic',
                            'underline'
                        ],
                        [
                            'blockquote'
                        ],
                        [
                            'link'
                        ],
                        [
                            {
                                'list':'ordered'
                            },
                            {
                                'list':'bullet'
                            }
                        ]
                    ]
                }
            });


        editor.root.innerHTML = content || "";


        this.editors[id] = editor;
    },


    getContent: function(id)
    {
        let editor = this.editors[id];

        if(!editor)
            return "";


        return editor.root.innerHTML;
    },

    cleanHtml: function(html)
    {
        let div = document.createElement("div");
        div.innerHTML = html;


        // Gmail Zitat entfernen verhindern
        let quotes = div.querySelectorAll(".gmail_quote");

        quotes.forEach(x => {
            x.style.borderLeft = "3px solid #888";
            x.style.paddingLeft = "15px";
            x.style.color = "#777";
        });


        return div.innerHTML;
    },

    setContent: function(id, content)
    {
        let editor = this.editors[id];

        if(!editor)
            return;


        let clean = this.cleanHtml(content);


        editor.clipboard.dangerouslyPasteHTML(
            clean
        );
    }

};