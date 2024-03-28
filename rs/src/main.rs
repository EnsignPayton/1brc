use itertools::Itertools;
use std::collections::HashMap;
use std::error::Error;
use std::fs::File;
use std::io::{BufRead as _, BufReader};

struct WeatherData {
    min: f32,
    max: f32,
    mean: f32,
    count: u64,
}

impl WeatherData {
    fn new(initial_value: f32) -> WeatherData {
        WeatherData {
            min: initial_value,
            max: initial_value,
            mean: initial_value,
            count: 1,
        }
    }

    fn update(&mut self, value: f32) {
        if value < self.min {
            self.min = value
        }
        if value > self.max {
            self.max = value
        }
        self.count += 1;

        let last_mean = self.mean;
        self.mean = last_mean + ((value - last_mean) / (self.count as f32));
    }
}

impl std::fmt::Display for WeatherData {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "{:.1}/{:.1}/{:.1}", self.min, self.mean, self.max)
    }
}

struct WeatherMap {
    data: HashMap<String, WeatherData>,
}

impl WeatherMap {
    fn new() -> WeatherMap {
        WeatherMap {
            data: HashMap::new(),
        }
    }

    fn update(&mut self, key: &str, value: f32) {
        if self.data.contains_key(key) {
            let weather_data: &mut WeatherData = self.data.get_mut(key).unwrap();
            weather_data.update(value);
        } else {
            self.data.insert(key.to_string(), WeatherData::new(value));
        }
    }
}

impl std::fmt::Display for WeatherMap {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "{{")?;

        let mut is_first = true;
        for key in self.data.keys().sorted() {
            if !is_first {
                write!(f, ", ")?;
            }

            write!(f, "{}={}", key, &self.data[key])?;

            is_first = false;
        }

        write!(f, "}}")
    }
}

fn parse_input<P>(input_path: P) -> Result<WeatherMap, Box<dyn Error>>
where
    P: AsRef<std::path::Path>,
{
    let mut map = WeatherMap::new();

    let file = File::open(input_path)?;
    let mut reader = BufReader::new(file);
    let mut line = String::new();

    while reader.read_line(&mut line)? > 0 {
        let i_sep = line.find(';').ok_or("")?;
        let key = line.get(0..i_sep).ok_or("")?;
        let value = line.get(i_sep + 1..).ok_or("")?.trim().parse::<f32>()?;

        map.update(key, value);
        line.clear();
    }

    Ok(map)
}

fn main() -> Result<(), Box<dyn Error>> {
    let input_path = std::env::args().nth(1).ok_or("")?;

    let start = std::time::SystemTime::now();
    let map = parse_input(input_path)?;
    let end = std::time::SystemTime::now();

    let duration = end.duration_since(start)?;

    println!("{}", map);
    println!("Processing took {:.3}s", duration.as_secs_f32());

    Ok(())
}
